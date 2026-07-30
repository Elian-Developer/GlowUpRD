import { apiRequest } from './apiClient'

export function obtenerReporte({ negocioId, desde, hasta }) {
  const params = new URLSearchParams({ negocioId: String(negocioId), desde, hasta })
  return apiRequest(`/api/reportes?${params.toString()}`)
}
