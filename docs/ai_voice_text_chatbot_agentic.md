# RetailSuite AI Assistant: Token-Optimized Hybrid Architecture
**Target Environment:** React (`retailsuite-web-retail`) & .NET/C# (`retailsuite-api`) with PostgreSQL (`pgvector` in `TenantDbContext`).
**Core LLM:** Gemini 2.5 Flash via REST API (Function Calling Enabled) & `text-embedding-004` (768 dimensions).

## 1. System Overview & Goals
Implement a multimodal (Voice + Text) AI assistant with a "Gemini Mobile" aesthetic. The assistant acts as an intelligent navigator and data-entry aid, operating strictly via a **Token-Optimized Pipeline**. 

To maintain near-zero token costs and strictly protect database integrity, the AI executes reads/writes via structured tool calling, but the .NET backend dictates exactly which tools the AI can see based on a **Hybrid Routing Engine** (UI Context + Semantic Search).

All platform AI tools and their precomputed embeddings are stored centrally in the global `TenantDbContext` (always PostgreSQL with `pgvector`), eliminating data duplication across tenants.

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
            VectorSearch["Semantic Intent Anchor<br/>(pgvector Cosine Search in TenantDbContext)"]
            Merge["Merge & Deduplicate Tools"]
        end
        
        ContextManager["History Pruner (Sliding Window & JSON Scrubber)"]
        GeminiService["GeminiAiAssistantService"]
        TenantDB["PostgreSQL Root DB<br/>TenantDbContext (AiTools with pgvector)"]
        AppDB["Tenant Business DB<br/>ApplicationDbContext (Vendors, Vouchers, Items)"]
    end

    subgraph External["Google Gemini API"]
        EmbeddingAPI["text-embedding-004 (768d)"]
        GeminiModel["Gemini 2.5 Flash"]
    end

    MicInput & TextInput --> Widget
    Widget -->|Prompt + Current Route `/vouchers` + History| AIController
    AIController --> ContextManager
    ContextManager --> HybridRouter
    UIContext --> Merge
    VectorSearch -->|Prompt Embedding| EmbeddingAPI
    EmbeddingAPI -->|768d Vector| TenantDB
    TenantDB -->|Top-2 Matching Tools| VectorSearch
    VectorSearch --> Merge
    Merge -->|~4 Targeted JSON schemas| GeminiService
    GeminiService -->|Prompt + Targeted Schemas + Pruned History| GeminiModel
    GeminiModel -->|Tool Call: search_vendor| GeminiService
    GeminiService -->|Executes Tenant-Scoped Read Query| AppDB
    AppDB -->|Slim JSON (ID, Name only)| GeminiService
    GeminiService -->|Database Result| GeminiModel
    GeminiModel -->|Final Action Payload + Spoken Text| AIController
    AIController --> Widget
    Widget --> Router
```

## 3. Token Optimization & Routing Strategy (Critical Path)

### 3.1. Hybrid Tool Routing (The Guardrail)
To prevent sending 50+ tool schemas to the LLM on every turn, the C# backend filters the schemas using two simultaneous anchors:
1. **The UI Anchor (O(1)):** Map the incoming `CurrentRoute` (e.g., `/daily-entries/payment-voucher`) to 2-3 static UI-specific schemas. This ensures the AI always understands the screen the user is looking at.
2. **The Semantic Anchor (pgvector in `TenantDbContext`):** Embed the user's prompt into a 768-dimensional vector using `text-embedding-004`. Query the global `AiTools` table in `TenantDbContext` sorting by `<=>` (Cosine Distance) with HNSW index. Take the top 2 matching schemas. This catches cross-domain requests (e.g., asking to create a voucher while on the inventory screen).
3. **The Merge:** Union both lists, remove duplicates, and attach ONLY these ~4 schemas to the Gemini API call.

### 3.2. Context Pruning
* **Sliding Window:** Keep only the last 4-6 conversational turns in the history array.
* **JSON Scrubbing:** Once the AI has successfully executed a backend tool and replied to the user, the .NET backend scrubs the raw Database JSON result out of the history, leaving only the AI's final natural language summary.

### 3.3. Data Guardrails (Human-in-the-Loop)
* **Reads:** AI directly queries the active tenant's `ApplicationDbContext` via EF Core (using `.Select()` projections to return slim objects like `[{id: 1042, name: "ABC Ventures"}]`).
* **Writes:** The AI **NEVER** executes direct `INSERT`/`UPDATE` mutations. It outputs a `DraftAction`. React Router pushes to the target form (e.g., `/daily-entries/payment-voucher/new`), injects the `DraftAction` payload into React state (`location.state?.draftData`), and waits for the human to review and click "Save".

## 4. Frontend Implementation Directives (`retailsuite-web-retail`)

### 4.1. Audio & UI (`GeminiAssistantWidget.tsx` & `useVoiceAssistant.ts`)
* **Zero-Cost Audio:** Use the browser's native `webkitSpeechRecognition` for Speech-to-Text and `window.speechSynthesis` for Text-to-Speech replies.
* **UI/UX:** A floating iridescent gradient pill button (bottom right). When active, displays a live sound wave using `AudioContext` and an `AnalyserNode`.
* **Action Cards:** Display temporary UI cards showing backend steps (e.g., *"Searching vendors..."* -> *"Drafting voucher..."*).

### 4.2. Routing Interface
* Catch AI `Navigate` and `Draft` commands. Use `useNavigate` from `react-router-dom`.
* Ensure Target pages (e.g., `PaymentVoucherList.tsx`, `PaymentVoucherForm.tsx`) are updated to read initialization data from query params and `location.state?.draftData`.

## 5. Backend Implementation Directives (`retailsuite-api`)

### 5.1. Database Setup (`TenantDbContext` with `pgvector`)
* Add `Pgvector.EntityFrameworkCore` to `retailsuite-api/src/Infrastructure/Infrastructure.csproj`.
* Create an `AiTool` entity in `Domain.Multitenancy`: `Id`, `ToolName`, `Description`, `Category`, `JsonSchema`, `RouteHint`, and `Vector Embedding (768)`.
* Configure `TenantDbContext` with `.HasPostgresExtension("vector")` and configure the vector index with `.HasMethod("hnsw")`.
* Run PostgreSQL migration in `Migrators.PostgreSQL` for `TenantDbContext`.

### 5.2. AI Services Setup
* **`EmbeddingService.cs`**: Calls Google `text-embedding-004` to generate 768-dimensional float vectors.
* **`ToolRouterService.cs`**: Implements the Hybrid Tool Routing logic (UI static map + `pgvector` semantic search union).
* **`ContextPrunerService.cs`**: Implements the history array truncating and JSON scrubbing.
* **`GeminiAiAssistantService.cs`**: Handles the recursive Gemini API loop (Send Request -> Catch Tool Call -> Execute EF Query on `ApplicationDbContext` -> Send Result to Gemini -> Return Final Action).
* **`AiAssistantController.cs`**: Exposed endpoint `POST /api/v1/ai/assistant/chat`.