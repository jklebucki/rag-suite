export interface User {
  id: string
  email: string
  name: string
  roles: string[]
  permissions: string[]
}

export interface AuthState {
  isAuthenticated: boolean
  user: User | null
  token: string | null
  loading: boolean
}

export interface LoginRequest {
  email: string
  password: string
}

export interface LoginResponse {
  token: string
  user: User
  expiresAt: Date
}
