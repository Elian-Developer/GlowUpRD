export const professionals = [
  { id: 'maria', name: 'María Rodríguez', shortName: 'María', initials: 'MR', specialty: 'Estilista', tone: 'mint' },
  { id: 'carlos', name: 'Carlos Reyes', shortName: 'Carlos', initials: 'CR', specialty: 'Barbero', tone: 'blue' },
  { id: 'lucia', name: 'Lucía Méndez', shortName: 'Lucía', initials: 'LM', specialty: 'Colorista', tone: 'sand' },
  { id: 'andres', name: 'Andrés Ruiz', shortName: 'Andrés', initials: 'AR', specialty: 'Barbero', tone: 'violet' },
]

export const services = [
  { id: 'classic-cut', name: 'Corte clásico', duration: 60, price: 850, category: 'Cabello' },
  { id: 'color', name: 'Coloración', duration: 90, price: 2500, category: 'Color' },
  { id: 'highlights', name: 'Mechas', duration: 120, price: 3200, category: 'Color' },
  { id: 'beard', name: 'Barba premium', duration: 60, price: 750, category: 'Barbería' },
  { id: 'cut-beard', name: 'Corte + barba', duration: 90, price: 1400, category: 'Barbería' },
  { id: 'treatment', name: 'Tratamiento capilar', duration: 60, price: 1800, category: 'Tratamientos' },
]

export const initialCustomers = [
  { id: 'c1', name: 'Juan Pérez', phone: '809-555-0101', email: 'juan@email.com', visits: 8 },
  { id: 'c2', name: 'Ana Gómez', phone: '809-555-0102', email: 'ana@email.com', visits: 12 },
  { id: 'c3', name: 'Laura Sánchez', phone: '829-555-0103', email: 'laura@email.com', visits: 5 },
  { id: 'c4', name: 'Pedro Ramírez', phone: '849-555-0104', email: 'pedro@email.com', visits: 16 },
  { id: 'c5', name: 'Miguel Torres', phone: '809-555-0105', email: 'miguel@email.com', visits: 3 },
  { id: 'c6', name: 'Sofía Martínez', phone: '829-555-0106', email: 'sofia@email.com', visits: 7 },
]

export function toDateKey(date) {
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}

export function createInitialAppointments() {
  const date = toDateKey(new Date())
  return [
    { id: crypto.randomUUID(), date, time: '09:00', duration: 60, professionalId: 'maria', customerId: 'c1', serviceId: 'classic-cut', status: 'confirmed', notes: '' },
    { id: crypto.randomUUID(), date, time: '10:00', duration: 90, professionalId: 'carlos', customerId: 'c2', serviceId: 'color', status: 'confirmed', notes: '' },
    { id: crypto.randomUUID(), date, time: '11:30', duration: 90, professionalId: 'lucia', customerId: 'c3', serviceId: 'highlights', status: 'pending', notes: '' },
    { id: crypto.randomUUID(), date, time: '13:00', duration: 60, professionalId: 'maria', customerId: 'c4', serviceId: 'beard', status: 'confirmed', notes: '' },
    { id: crypto.randomUUID(), date, time: '14:30', duration: 90, professionalId: 'andres', customerId: 'c5', serviceId: 'cut-beard', status: 'pending', notes: '' },
    { id: crypto.randomUUID(), date, time: '16:00', duration: 60, professionalId: 'carlos', customerId: 'c6', serviceId: 'treatment', status: 'confirmed', notes: '' },
  ]
}
