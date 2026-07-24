import http from "node:http";

const host = process.env.LOCAL_AI_HOST ?? "127.0.0.1";
const port = Number(process.env.LOCAL_AI_PORT ?? 8788);
const ollamaUrl = (process.env.OLLAMA_URL ?? "http://127.0.0.1:11434").replace(/\/$/, "");
const model = process.env.OLLAMA_MODEL ?? "qwen3:0.6b";
const maxBodyBytes = 128 * 1024;
const allowedOrigins = new Set(
  (process.env.LOCAL_AI_ALLOWED_ORIGINS ?? "https://movie.ziet.dev,http://localhost:3000,http://127.0.0.1:3000")
    .split(",")
    .map((origin) => origin.trim())
    .filter(Boolean),
);

function corsHeaders(request) {
  const origin = request?.headers?.origin;
  return {
    ...(origin && allowedOrigins.has(origin) ? { "access-control-allow-origin": origin, vary: "Origin" } : {}),
    "access-control-allow-methods": "GET, POST, OPTIONS",
    "access-control-allow-headers": "content-type",
  };
}

function sendJson(response, status, body, request) {
  const payload = JSON.stringify(body);
  response.writeHead(status, {
    ...corsHeaders(request),
    "content-type": "application/json; charset=utf-8",
    "content-length": Buffer.byteLength(payload),
  });
  response.end(payload);
}

async function readJson(request) {
  const chunks = [];
  let size = 0;
  for await (const chunk of request) {
    size += chunk.length;
    if (size > maxBodyBytes) throw new Error("request_too_large");
    chunks.push(chunk);
  }
  return JSON.parse(Buffer.concat(chunks).toString("utf8"));
}

function buildPrompt(input) {
  const locale = input.locale === "vi" ? "vi" : "en";
  const language = locale === "vi" ? "Vietnamese" : "English";
  const matches = Array.isArray(input.matches) ? input.matches.slice(0, 8) : [];
  const catalog = matches.map((item) => ({
    slug: item?.title?.slug,
    title: item?.title?.title,
    genre: item?.title?.genre,
    year: item?.title?.year,
    type: item?.title?.type,
    synopsis: item?.synopsis,
  }));
  return {
    model,
    stream: false,
    think: false,
    messages: [
      {
        role: "system",
        content: `You are ZMovie's movie assistant. Answer in ${language}. CATALOG is only context for the real movie cards rendered by the app. Do not name, list, number, or invent any movie, actor, year, or fact. Return one short plain-text explanation of why the retrieved suggestions fit the request. Never return JSON. Keep it under 40 words.`,
      },
      {
        role: "user",
        content: `USER REQUEST: ${String(input.message ?? "").trim()}\nCATALOG (retrieved from the user's watch history and saved-title profile):\n${JSON.stringify(catalog)}`,
      },
    ],
    options: { temperature: 0.2, num_predict: 160 },
  };
}

async function handleChat(request, response) {
  let input;
  try {
    input = await readJson(request);
  } catch (error) {
    sendJson(response, error.message === "request_too_large" ? 413 : 400, { error: "invalid_request" }, request);
    return;
  }

  if (typeof input.message !== "string" || input.message.trim().length === 0 || input.message.length > 500) {
    sendJson(response, 400, { error: "message_required" }, request);
    return;
  }

  try {
    const ollamaResponse = await fetch(`${ollamaUrl}/api/chat`, {
      method: "POST",
      headers: { "content-type": "application/json" },
      body: JSON.stringify(buildPrompt(input)),
      signal: AbortSignal.timeout(12000),
    });
    if (!ollamaResponse.ok) {
      sendJson(response, 502, { error: "ollama_error" }, request);
      return;
    }
    const result = await ollamaResponse.json();
    const reply = typeof result?.message?.content === "string" ? result.message.content.trim() : "";
    if (!reply) {
      sendJson(response, 502, { error: "empty_model_reply" }, request);
      return;
    }
    sendJson(response, 200, { reply }, request);
  } catch {
    sendJson(response, 503, { error: "ollama_unavailable" }, request);
  }
}

const server = http.createServer((request, response) => {
  if (request.method === "OPTIONS") {
    response.writeHead(204, corsHeaders(request));
    response.end();
    return;
  }
  if (request.method === "GET" && request.url === "/health") {
    sendJson(response, 200, { status: "ok", model, ollamaUrl }, request);
    return;
  }
  if (request.method === "POST" && request.url === "/v1/chat") {
    handleChat(request, response);
    return;
  }
  sendJson(response, 404, { error: "not_found" }, request);
});

server.listen(port, host, () => {
  console.log(`ZMovie local AI listening on http://${host}:${port} using ${model}`);
});
