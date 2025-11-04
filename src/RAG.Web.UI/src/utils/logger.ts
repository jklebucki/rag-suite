/**
 * Centralized logging utility
 * Provides consistent logging with environment-based filtering
 */

type LogLevel = 'debug' | 'info' | 'warn' | 'error'

interface Logger {
  debug: (message: string, ...args: unknown[]) => void
  info: (message: string, ...args: unknown[]) => void
  warn: (message: string, ...args: unknown[]) => void
  error: (message: string, ...args: unknown[]) => void
}

const isDevelopment = import.meta.env.DEV
const isProduction = import.meta.env.PROD

/**
 * Logs a message at the specified level
 */
function log(level: LogLevel, message: string, ...args: unknown[]): void {
  // In production, only log errors and warnings
  if (isProduction && (level === 'debug' || level === 'info')) {
    return
  }

  const timestamp = new Date().toISOString()
  const prefix = `[${timestamp}] [${level.toUpperCase()}]`

  switch (level) {
    case 'debug':
      console.debug(prefix, message, ...args)
      break
    case 'info':
      console.info(prefix, message, ...args)
      break
    case 'warn':
      console.warn(prefix, message, ...args)
      break
    case 'error':
      console.error(prefix, message, ...args)
      break
  }
}

/**
 * Centralized logger instance
 */
export const logger: Logger = {
  debug: (message: string, ...args: unknown[]) => {
    if (isDevelopment) {
      log('debug', message, ...args)
    }
  },
  info: (message: string, ...args: unknown[]) => {
    log('info', message, ...args)
  },
  warn: (message: string, ...args: unknown[]) => {
    log('warn', message, ...args)
  },
  error: (message: string, ...args: unknown[]) => {
    log('error', message, ...args)
  },
}

/**
 * Creates a scoped logger with a prefix
 */
export function createScopedLogger(scope: string): Logger {
  return {
    debug: (message: string, ...args: unknown[]) => {
      logger.debug(`[${scope}] ${message}`, ...args)
    },
    info: (message: string, ...args: unknown[]) => {
      logger.info(`[${scope}] ${message}`, ...args)
    },
    warn: (message: string, ...args: unknown[]) => {
      logger.warn(`[${scope}] ${message}`, ...args)
    },
    error: (message: string, ...args: unknown[]) => {
      logger.error(`[${scope}] ${message}`, ...args)
    },
  }
}

