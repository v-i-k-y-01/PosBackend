import { X } from 'lucide-react';
import type { ReactNode } from 'react';

/**
 * Properties structure for the Modal component.
 */
interface ModalProps {
  title: string;
  children: ReactNode;
  onClose: () => void;
}

/**
 * Reusable modal overlay dialog component.
 */
export function Modal({ title, children, onClose }: ModalProps) {
  return (
    <div className="modal-backdrop" onMouseDown={onClose}>
      <section className="modal" onMouseDown={(event) => event.stopPropagation()}>
        <button className="icon-button close" onClick={onClose} title="Close dialog">
          <X size={20} />
        </button>
        <h2>{title}</h2>
        {children}
      </section>
    </div>
  );
}
