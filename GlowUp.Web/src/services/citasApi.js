import { apiRequest } from './apiClient'

export function obtenerNegocios() {
  return apiRequest('/api/citas/negocios')
}

export function obtenerCatalogos(negocioId, sucursalId) {
  const params = new URLSearchParams({ negocioId: String(negocioId) })
  if (sucursalId) params.set('sucursalId', String(sucursalId))
  return apiRequest(`/api/citas/catalogos?${params.toString()}`)
}

export function buscarCitas({ negocioId, desde, hasta, sucursalId, empleadoId }) {
  const params = new URLSearchParams({
    negocioId: String(negocioId),
    desde,
    hasta,
  })

  if (sucursalId) params.set('sucursalId', String(sucursalId))
  if (empleadoId) params.set('empleadoId', String(empleadoId))

  return apiRequest(`/api/citas?${params.toString()}`)
}

export function crearCita(payload) {
  return apiRequest('/api/citas', {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}

export function actualizarCita(id, payload) {
  return apiRequest(`/api/citas/${id}`, {
    method: 'PUT',
    body: JSON.stringify(payload),
  })
}

export function eliminarCita(id) {
  return apiRequest(`/api/citas/${id}`, {
    method: 'DELETE',
  })
}
