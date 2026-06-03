import axios from 'axios'

const api = axios.create({
  baseURL: (import.meta.env.VITE_API_URL || 'http://localhost:5010') + '/api'
})

// injeta JWT em toda requisição
api.interceptors.request.use((config) => {
  const token = localStorage.getItem('token')
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

// redireciona pro login se expirar
api.interceptors.response.use(
  (r) => r,
  (err) => {
    if (err.response?.status === 401) {
      localStorage.removeItem('token')
      if (!location.pathname.startsWith('/agendar')) location.href = '/login'
    }
    return Promise.reject(err)
  }
)

export default api
