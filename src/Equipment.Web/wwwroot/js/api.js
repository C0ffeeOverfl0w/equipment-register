export class ApiError extends Error {
    constructor(status, problem) {
        super(problem?.title ?? "Не удалось выполнить запрос.");
        this.status = status;
        this.code = problem?.code;
        this.errors = problem?.errors ?? {};
    }
}

export async function requestJson(url, { method = "GET", body, signal } = {}) {
    const response = await fetch(url, {
        method,
        signal,
        headers: {
            Accept: "application/json, application/problem+json",
            ...(body === undefined ? {} : { "Content-Type": "application/json" })
        },
        body: body === undefined ? undefined : JSON.stringify(body)
    });
    if (!response.ok) {
        let problem;
        try { problem = await response.json(); } catch { }
        throw new ApiError(response.status, problem);
    }

    return response.json();
}
