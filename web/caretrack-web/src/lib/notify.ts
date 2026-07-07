import { toast } from 'react-toastify'
import { getErrorMessage } from '@/lib/api-client'

export const notify = {
  success: (message: string) => toast.success(message),
  error: (error: unknown) => toast.error(getErrorMessage(error)),
  info: (message: string) => toast.info(message),
}
