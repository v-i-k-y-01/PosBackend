export function Toast({ message, type = 'success' }: { message: string; type?: 'success' | 'error' }) { return <div className={`toast ${type}`}>{message}</div>; }
