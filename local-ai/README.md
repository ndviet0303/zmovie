# ZMovie local AI

This is a local-only adapter between the ZMovie API and Ollama. It keeps the
model runtime out of the backend deployment.

## Run

Install Ollama, then pull the small local model once:

```bash
ollama serve
ollama pull qwen3:0.6b
```

In another terminal:

```bash
cd local-ai
npm start
```

The service binds to `127.0.0.1:8788` and proxies generation to Ollama at
`127.0.0.1:11434`. Override `LOCAL_AI_PORT`, `OLLAMA_URL`, or `OLLAMA_MODEL`
only for local development.
