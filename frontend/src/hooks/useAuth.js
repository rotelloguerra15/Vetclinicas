import { useState, useEffect } from 'react'
import api from '../api/client'

export function useAuth() {
  const [user, setUser] = useState(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    const nome = localStorage.getItem('nome')
    const papel = localStorage.getItem('papel')
    if (localStorage.getItem('token') && nome) setUser({ nome, papel })
    setLoading(false)
  }, [])

  async function login(email, senha) {
    const { data } = await api.post('/auth/login', { email, senha })
    localStorage.setItem('token', data.token)
    localStorage.setItem('nome', data.nome)
    localStorage.setItem('papel', data.papel)
    setUser({ nome: data.nome, papel: data.papel })
    return data
  }

  function logout() {
    localStorage.clear()
    setUser(null)
    location.href = '/login'
  }

  return { user, loading, login, logout }
}
