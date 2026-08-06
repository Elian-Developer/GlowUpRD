import { apiRequest } from './apiClient'

export function buscarEmpleados({ negocioId, incluirInactivos }) {
  const params = new URLSearchParams({ negocioId: String(negocioId) })
  if (incluirInactivos) params.set('incluirInactivos', 'true')
  return apiRequest(`/api/empleados?${params.toString()}`)
}

export function obtenerEmpleado(id) {
  return apiRequest(`/api/empleados/${id}`)
}

export function crearEmpleado(payload) {
  return apiRequest('/api/empleados', {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}

export function actualizarEmpleado(id, payload) {
  return apiRequest(`/api/empleados/${id}`, {
    method: 'PUT',
    body: JSON.stringify(payload),
  })
}

export function eliminarEmpleado(id) {
  return apiRequest(`/api/empleados/${id}`, {
    method: 'DELETE',
  })
}
