import { X } from 'lucide-react';
import type { ReactNode } from 'react';
export function Modal({ title, children, onClose }: { title: string; children: ReactNode; onClose: () => void }) { return <div className="modal-backdrop" onMouseDown={onClose}><section className="modal" onMouseDown={(event) => event.stopPropagation()}><button className="icon-button close" onClick={onClose}><X size={20} /></button><h2>{title}</h2>{children}</section></div>; }
