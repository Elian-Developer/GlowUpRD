import { apiRequest } from './apiClient'

export function buscarServicios({ negocioId, incluirInactivos }) {
  const params = new URLSearchParams({ negocioId: String(negocioId) })
  if (incluirInactivos) params.set('incluirInactivos', 'true')
  return apiRequest(`/api/servicios?${params.toString()}`)
}

export function crearServicio(payload) {
  return apiRequest('/api/servicios', {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}

export function actualizarServicio(id, payload) {
  return apiRequest(`/api/servicios/${id}`, {
    method: 'PUT',
    body: JSON.stringify(payload),
  })
}

export function eliminarServicio(id) {
  return apiRequest(`/api/servicios/${id}`, {
    method: 'DELETE',
  })
}
