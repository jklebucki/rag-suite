// All code comments must be written in English, regardless of the conversation language.

import { useState, useEffect, useMemo } from 'react'
import type { User } from '@/types/auth'
import type { UserFilters } from '../types/settings'

/**
 * Custom hook for managing user filtering logic and URL persistence
 */
export function useUserFilters(users: User[] = []) {
  const [filters, setFilters] = useState<UserFilters>({
    searchText: '',
    selectedRoles: [],
    activeStatus: 'all',
    createdDateFrom: '',
    createdDateTo: '',
    lastLoginFrom: '',
    lastLoginTo: ''
  })

  const [debouncedSearchText, setDebouncedSearchText] = useState('')

  // Debounce search text
  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedSearchText(filters.searchText)
    }, 300)

    return () => clearTimeout(timer)
  }, [filters.searchText])

  // Load filters from URL on mount
  useEffect(() => {
    const urlParams = new URLSearchParams(window.location.search)

    const searchText = urlParams.get('search') || ''
    const selectedRoles = urlParams.get('roles')?.split(',').filter(Boolean) || []
    const activeStatus = (urlParams.get('status') as 'all' | 'active' | 'inactive') || 'all'
    const createdDateFrom = urlParams.get('createdFrom') || ''
    const createdDateTo = urlParams.get('createdTo') || ''
    const lastLoginFrom = urlParams.get('loginFrom') || ''
    const lastLoginTo = urlParams.get('loginTo') || ''

    setFilters({
      searchText,
      selectedRoles,
      activeStatus,
      createdDateFrom,
      createdDateTo,
      lastLoginFrom,
      lastLoginTo
    })
  }, [])

  // Update URL when filters change
  useEffect(() => {
    const urlParams = new URLSearchParams()

    if (filters.searchText) urlParams.set('search', filters.searchText)
    if (filters.selectedRoles.length > 0) urlParams.set('roles', filters.selectedRoles.join(','))
    if (filters.activeStatus !== 'all') urlParams.set('status', filters.activeStatus)
    if (filters.createdDateFrom) urlParams.set('createdFrom', filters.createdDateFrom)
    if (filters.createdDateTo) urlParams.set('createdTo', filters.createdDateTo)
    if (filters.lastLoginFrom) urlParams.set('loginFrom', filters.lastLoginFrom)
    if (filters.lastLoginTo) urlParams.set('loginTo', filters.lastLoginTo)

    const newUrl = urlParams.toString()
    const currentUrl = window.location.search.substring(1)

    if (newUrl !== currentUrl) {
      window.history.replaceState(null, '', newUrl ? `?${newUrl}` : window.location.pathname)
    }
  }, [filters])

  // Filter users based on current filters
  const filteredUsers = useMemo(() => {
    if (!users || !Array.isArray(users)) return []

    return users.filter(user => {
      if (!user) return false

      // Text search filter
      if (filters.searchText) {
        const searchLower = filters.searchText.toLowerCase()
        const matchesSearch =
          user.firstName?.toLowerCase().includes(searchLower) ||
          user.lastName?.toLowerCase().includes(searchLower) ||
          user.fullName?.toLowerCase().includes(searchLower) ||
          user.userName?.toLowerCase().includes(searchLower) ||
          user.email?.toLowerCase().includes(searchLower)

        if (!matchesSearch) return false
      }

      // Role filter
      if (filters.selectedRoles.length > 0) {
        const userRoles = user.roles || []
        const hasAllSelectedRoles = filters.selectedRoles.every(role =>
          userRoles.includes(role)
        )
        if (!hasAllSelectedRoles) return false
      }

      // Active status filter
      if (filters.activeStatus !== 'all') {
        if (filters.activeStatus === 'active' && !user.isActive) return false
        if (filters.activeStatus === 'inactive' && user.isActive) return false
      }

      // Created date filter
      if (filters.createdDateFrom) {
        const createdDate = new Date(user.createdAt)
        const fromDate = new Date(filters.createdDateFrom)
        if (createdDate < fromDate) return false
      }
      if (filters.createdDateTo) {
        const createdDate = new Date(user.createdAt)
        const toDate = new Date(filters.createdDateTo)
        toDate.setHours(23, 59, 59, 999) // End of day
        if (createdDate > toDate) return false
      }

      // Last login filter
      if (filters.lastLoginFrom) {
        if (!user.lastLoginAt) return false
        const lastLoginDate = new Date(user.lastLoginAt)
        const fromDate = new Date(filters.lastLoginFrom)
        if (lastLoginDate < fromDate) return false
      }
      if (filters.lastLoginTo) {
        if (!user.lastLoginAt) return false
        const lastLoginDate = new Date(user.lastLoginAt)
        const toDate = new Date(filters.lastLoginTo)
        toDate.setHours(23, 59, 59, 999) // End of day
        if (lastLoginDate > toDate) return false
      }

      return true
    })
  }, [users, filters])

  const clearFilters = () => {
    setFilters({
      searchText: '',
      selectedRoles: [],
      activeStatus: 'all',
      createdDateFrom: '',
      createdDateTo: '',
      lastLoginFrom: '',
      lastLoginTo: ''
    })
    setDebouncedSearchText('')
  }

  const applyDatePreset = (field: 'created' | 'lastLogin', days: number) => {
    const today = new Date()
    const fromDate = new Date(today)
    fromDate.setDate(today.getDate() - days)

    if (field === 'created') {
      setFilters(prev => ({
        ...prev,
        createdDateFrom: fromDate.toISOString().split('T')[0],
        createdDateTo: today.toISOString().split('T')[0]
      }))
    } else {
      setFilters(prev => ({
        ...prev,
        lastLoginFrom: fromDate.toISOString().split('T')[0],
        lastLoginTo: today.toISOString().split('T')[0]
      }))
    }
  }

  return {
    filters,
    setFilters,
    filteredUsers,
    clearFilters,
    applyDatePreset,
    debouncedSearchText
  }
}
