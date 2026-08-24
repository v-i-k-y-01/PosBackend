import type { ReactNode } from 'react';

/**
 * Props for the EmptyState component.
 */
interface EmptyStateProps {
  icon: ReactNode;
  title: string;
  detail: string;
}

/**
 * Component displayed when no data results are found or catalog lists are empty.
 */
export function EmptyState({ icon, title, detail }: EmptyStateProps) {
  return (
    <div className="empty-state">
      <span>{icon}</span>
      <h3>{title}</h3>
      <p>{detail}</p>
    </div>
  );
}
