/**
 * Props for the Toast component.
 */
interface ToastProps {
  message: string;
  type?: 'success' | 'error';
}

/**
 * Toast notification banner displaying temporary alert messages.
 */
export function Toast({ message, type = 'success' }: ToastProps) {
  return (
    <div className={`toast ${type}`}>
      {message}
    </div>
  );
}
