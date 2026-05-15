function normalizeApiBaseUrl(value) {
  const trimmedUrl = String(value || '').replace(/\/+$/, '')

  if (!trimmedUrl) {
    return null
  }

  if (/\/api\/v\d+$/i.test(trimmedUrl)) {
    return trimmedUrl
  }

  return `${trimmedUrl}/api/v1`
}

function proxiedApiRequest(request, apiBaseUrl) {
  const sourceUrl = new URL(request.url)
  const targetBase = new URL(apiBaseUrl)
  const targetUrl = new URL(targetBase.origin)
  const incomingApiPath = sourceUrl.pathname.replace(/^\/api\/v\d+/i, '')

  targetUrl.pathname = `${targetBase.pathname}${incomingApiPath}`.replace(/\/{2,}/g, '/')
  targetUrl.search = sourceUrl.search

  const headers = new Headers(request.headers)
  headers.delete('host')

  return new Request(targetUrl, {
    method: request.method,
    headers,
    body: request.body,
    redirect: 'manual',
  })
}

export default {
  async fetch(request, env) {
    const url = new URL(request.url)

    if (url.pathname.startsWith('/api/')) {
      const apiBaseUrl = normalizeApiBaseUrl(env.API_ORIGIN)

      if (!apiBaseUrl) {
        return Response.json(
          {
            message: 'Missing API_ORIGIN runtime variable.',
          },
          { status: 500 },
        )
      }

      return fetch(proxiedApiRequest(request, apiBaseUrl))
    }

    return env.ASSETS.fetch(request)
  },
}
