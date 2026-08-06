import { apiRequest } from './apiClient'

export function obtenerReporte({ negocioId, desde, hasta, sucursalId }) {
  const params = new URLSearchParams({ negocioId: String(negocioId), desde, hasta })
  if (sucursalId) params.set('sucursalId', String(sucursalId))
  return apiRequest(`/api/reportes?${params.toString()}`)
}
