console.log('CacheSwaggerPayloads.js is loaded');

window.addEventListener('DOMContentLoaded', function () {
    console.log('CacheSwaggerPayloads.js is ready');
    console.log(window);

    const getProjectName = () => {
        const spec = window?.ui?.getConfigs?.()?.spec || {};
        return spec?.info?.title?.replace(/\s+/g, '_') || 'demo';
    };

    const projectName = "demo";
    const CACHE_KEY_PREFIX = `swagger-payload-${projectName}-`;

    const overrideExecute = function () {
        const executeButtons = document.querySelectorAll('.btn.execute');
        executeButtons.forEach((btn) => {
            btn.addEventListener('click', () => {
                const opBlock = btn.closest('.opblock');
                const operationId = opBlock.getElementsByClassName('opblock-summary-path')[0].attributes["data-path"].textContent;
                const textarea = opBlock.getElementsByClassName("body-param__text")[0].textContent;
                if (operationId && textarea) {
                    const payload = textarea;
                    if (payload) {
                        const cacheKey = `${CACHE_KEY_PREFIX}${operationId}`;
                        localStorage.setItem(cacheKey, payload);
                    }
                }
            });
        });
    };

    const overrideTryItOut = function () {
        const tryItOutButtons = document.querySelectorAll('.btn.try-out__btn');
        tryItOutButtons.forEach((btn) => {
            btn.addEventListener('click', () => {
                const opBlock = btn.closest('.opblock');
                if (!opBlock) {
                    return;
                }
                console.log(opBlock);

                const operationIdElement = opBlock.getElementsByClassName('opblock-summary-path')[0];
                if (!operationIdElement) {
                    console.log("operationId element not found");
                    return;
                }
                const operationId = operationIdElement.attributes["data-path"]?.textContent;

                if (operationId) {
                    const cacheKey = `${CACHE_KEY_PREFIX}${operationId}`;
                    const sectionRequestBody = opBlock.getElementsByClassName("opblock-section-request-body")[0];
                    if (!sectionRequestBody) {
                        console.log("sectionRequestBody not found");
                        return;
                    }

                    const wrapper = sectionRequestBody.getElementsByClassName("opblock-description-wrapper")[0];
                    if (!wrapper) {
                        console.log("Wrapper not found");
                        return;
                    }

                    let textarea = wrapper.getElementsByClassName("body-param__text")[0];

                    setTimeout(() => {
                        textarea = wrapper.getElementsByClassName("body-param__text")[0];
                        console.log("Textarea found:", textarea);

                        if (!textarea) {
                            console.log("Textarea not found");
                            return;
                        }

                        const cachedPayload = localStorage.getItem(cacheKey);

                        if (cachedPayload) {
                            textarea.value = cachedPayload;
                            console.log("Payload restored to textarea");
                        }

                    }, 0);
                }
            });
        });
    };

    const observer = new MutationObserver(() => {
        overrideExecute();
        overrideTryItOut();
    });

    observer.observe(document.body, { childList: true, subtree: true });
});
