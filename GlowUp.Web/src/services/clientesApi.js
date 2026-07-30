import { apiRequest } from './apiClient'

export function buscarClientes({ negocioId, incluirInactivos }) {
  const params = new URLSearchParams({ negocioId: String(negocioId) })
  if (incluirInactivos) params.set('incluirInactivos', 'true')
  return apiRequest(`/api/clientes?${params.toString()}`)
}

export function crearCliente(payload) {
  return apiRequest('/api/clientes', {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}

export function actualizarCliente(id, payload) {
  return apiRequest(`/api/clientes/${id}`, {
    method: 'PUT',
    body: JSON.stringify(payload),
  })
}

export function eliminarCliente(id) {
  return apiRequest(`/api/clientes/${id}`, {
    method: 'DELETE',
  })
}
