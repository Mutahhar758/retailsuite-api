# RetailSuite AI Assistant: Token-Optimized Hybrid Architecture
**Target Environment:** React (`retailsuite-web-retail`) & .NET/C# (`retailsuite-api`) with PostgreSQL (`pgvector`).
**Core LLM:** Gemini 2.5 Flash via REST API (Function Calling Enabled).

## 1. System Overview & Goals
Implement a multimodal (Voice + Text) AI assistant with a "Gemini Mobile" aesthetic. The assistant acts as an intelligent navigator and data-entry aid, operating strictly via a **Token-Optimized Pipeline**. 

To maintain near-zero token costs and strictly protect database integrity, the AI executes reads/writes via structured tool calling, but the .NET backend dictates exactly which tools the AI can see based on a **Hybrid Routing Engine** (UI Context + Semantic Search).

## 2. High-Level Architecture Flow

```mermaid
flowchart TD
    subgraph Client["retailsuite-web-retail (React Frontend)"]
        MicInput["🎤 Voice / VAD"]
        TextInput["⌨️ Text Prompt"]
        Widget["GeminiAssistantWidget.tsx"]
        Router["React Router DOM"]
    end

    subgraph Backend[".NET Backend (retailsuite-api)"]
        AIController["AiAssistantController"]
        
        subgraph HybridRouter["🧠 Hybrid Tool Routing Engine"]
            UIContext["UI Context Anchor<br/>(Reads React URL Route)"]
            VectorSearch["Semantic Intent Anchor<br/>(pgvector Cosine Search)"]
            Merge["Merge & Deduplicate Tools"]
        end
        
        ContextManager["History Pruner (Sliding Window & JSON Scrubber)"]
        GeminiService["GeminiAiAssistantService"]
        DB["PostgreSQL / pgvector"]
    end

    subgraph External["Google Gemini API"]
        GeminiModel["Gemini 2.5 Flash"]
    end

    MicInput & TextInput --> Widget
    Widget -->|Prompt + Current Route `/vouchers` + History| AIController
    AIController --> ContextManager
    ContextManager --> HybridRouter
    UIContext --> Merge
    VectorSearch --> Merge
    Merge -->|~4 Targeted JSON schemas| GeminiService
    GeminiService -->|Prompt + Targeted Schemas + Pruned History| GeminiModel
    GeminiModel -->|Tool Call: search_vendor| GeminiService
    GeminiService -->|Executes Query| DB
    DB -->|Slim JSON (ID, Name only)| GeminiService
    GeminiService -->|Database Result| GeminiModel
    GeminiModel -->|Final Action Payload + Spoken Text| AIController
    AIController --> Widget
    Widget --> Router
```

## 3. Token Optimization & Routing Strategy (Critical Path)

### 3.1. Hybrid Tool Routing (The Guardrail)
To prevent sending 50+ tool schemas to the LLM on every turn, the C# backend filters the schemas using two simultaneous anchors:
1. **The UI Anchor (O(1)):** Map the incoming `CurrentRoute` (e.g., `/inventory`) to 2-3 static UI-specific schemas. This ensures the AI always understands the screen the user is looking at.
2. **The Semantic Anchor (pgvector):** Embed the user's prompt into a vector. Query the `AiTools` PostgreSQL table sorting by `<=>` (Cosine Distance). Take the top 2 matching schemas. This catches cross-domain requests (e.g., asking to create a voucher while on the inventory screen).
3. **The Merge:** Union both lists, remove duplicates, and attach ONLY these ~4 schemas to the Gemini API call.

### 3.2. Context Pruning
* **Sliding Window:** Keep only the last 4-6 conversational turns in the history array.
* **JSON Scrubbing:** Once the AI has successfully executed a backend tool and replied to the user, the .NET backend scrubs the raw Database JSON result out of the history, leaving only the AI's final natural language summary.

### 3.3. Data Guardrails (Human-in-the-Loop)
* **Reads:** AI directly queries the database via EF Core (using `.Select()` projections to return slim objects like `[{id: 1, name: "ABC"}]`).
* **Writes:** The AI **NEVER** executes `INSERT`/`UPDATE` on the database. It outputs a `DraftAction`. React Router pushes to the target form (e.g., `/vouchers/new`), injects the `DraftAction` payload into React state, and waits for the human to click "Save".

## 4. Frontend Implementation Directives (`retailsuite-web-retail`)

### 4.1. Audio & UI (`GeminiAssistantWidget.tsx` & `useVoiceAssistant.ts`)
* **Zero-Cost Audio:** Use the browser's native `webkitSpeechRecognition` for Speech-to-Text and `window.speechSynthesis` for Text-to-Speech replies.
* **UI/UX:** A floating iridescent gradient pill button (bottom right). When active, displays a live sound wave using `AudioContext` and an `AnalyserNode`.
* **Action Cards:** Display temporary UI cards showing backend steps (e.g., *"Searching vendors..."* -> *"Drafting voucher..."*).

### 4.2. Routing Interface
* Catch AI `Navigate` and `Draft` commands. Use `useNavigate` from `react-router-dom`.
* Ensure Target pages (e.g., `PaymentVoucherForm.tsx`) are updated to read initialization data from `location.state?.draftData`.

## 5. Backend Implementation Directives (`retailsuite-api`)

### 5.1. Database Setup (`pgvector`)
* Add `Pgvector.EntityFrameworkCore` to the project.
* Create an `AiTool` entity with properties: `Id`, `TenantId`, `ToolName`, `JsonSchema`, and `[Column(TypeName = "vector(1536)")] Vector Embedding`.
* Configure the DbContext with `.UseVector()` and `.HasMethod("hnsw")`.

### 5.2. AI Services Setup
* **`ToolRouterService.cs`**: Implements the Hybrid Tool Routing logic (UI static map + `pgvector` semantic search union).
* **`ContextPrunerService.cs`**: Implements the history array truncating and JSON scrubbing.
* **`GeminiAiAssistantService.cs`**: Handles the recursive Gemini API loop (Send Request -> Catch Tool Call -> Execute EF Query -> Send Result to Gemini -> Return Final Action).

## 6. Execution Loop Example
**User:** *"I'm on the Dashboard. Create a $500 payment voucher for ABC Ventures."*
1. **React:** Sends `{ prompt: "...", route: "/" }`.
2. **.NET Router:** UI route `/` yields no specific tools. `pgvector` search yields `create_payment_voucher` and `search_vendor` schemas.
3. **.NET AI Service:** Sends prompt + 2 schemas to Gemini.
4. **Gemini:** Returns tool call `search_vendor(name: "ABC Ventures")`.
5. **.NET DB:** EF Core finds ID 1042. Returns `{"id": 1042, "name": "ABC Ventures"}` to Gemini.
6. **Gemini:** Returns frontend tool call `draft_voucher(vendor_id: 1042, amount: 500)` and text *"I've drafted the payment voucher for $500. Please review and save."*
7. **React:** Speaks text via TTS, navigates to `/daily-entries/payment-voucher/new`, and pre-fills the form fields.