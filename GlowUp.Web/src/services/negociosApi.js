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

export function obtenerMiembros(id) {
  return apiRequest(`/api/negocios/${id}/usuarios`)
}

export function crearUsuarioNegocio(id, payload) {
  return apiRequest(`/api/negocios/${id}/usuarios`, {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}
