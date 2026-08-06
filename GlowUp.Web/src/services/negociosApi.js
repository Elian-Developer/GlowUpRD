import { apiRequest } from './apiClient'

export function registrarNegocio(payload) {
  return apiRequest('/api/negocios', {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}

export function obtenerNegocio(id) {
  return apiRequest(`/api/negocios/${id}`)
}

export function actualizarNegocio(id, payload) {
  return apiRequest(`/api/negocios/${id}`, {
    method: 'PUT',
    body: JSON.stringify(payload),
  })
}

export function obtenerSucursal(negocioId, sucursalId) {
  return apiRequest(`/api/negocios/${negocioId}/sucursales/${sucursalId}`)
}

export function crearSucursal(negocioId, payload) {
  return apiRequest(`/api/negocios/${negocioId}/sucursales`, { method: 'POST', body: JSON.stringify(payload) })
}

export function actualizarSucursal(negocioId, sucursalId, payload) {
  return apiRequest(`/api/negocios/${negocioId}/sucursales/${sucursalId}`, { method: 'PUT', body: JSON.stringify(payload) })
}

export function desactivarSucursal(negocioId, sucursalId) {
  return apiRequest(`/api/negocios/${negocioId}/sucursales/${sucursalId}`, { method: 'DELETE' })
}

export function reactivarSucursal(negocioId, sucursalId) {
  return apiRequest(`/api/negocios/${negocioId}/sucursales/${sucursalId}/reactivar`, { method: 'POST' })
}

export function marcarSucursalPrincipal(negocioId, sucursalId) {
  return apiRequest(`/api/negocios/${negocioId}/sucursales/${sucursalId}/principal`, { method: 'POST' })
}

export function obtenerMiembros(id) {
  return apiRequest(`/api/negocios/${id}/usuarios`)
}

export function crearUsuarioNegocio(id, payload) {
  return apiRequest(`/api/negocios/${id}/usuarios`, {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}
