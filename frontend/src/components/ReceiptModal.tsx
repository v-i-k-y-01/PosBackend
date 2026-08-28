import { useEffect } from 'react';
import { CheckCircle2, Printer, ShoppingBag, X } from 'lucide-react';
import type { Sale } from '../api/types';

interface ReceiptModalProps {
  sale: Sale | null;
  onClose: () => void;
}

/**
 * Currency formatter configured for Indian Rupees (INR).
 */
const moneyFormatter = new Intl.NumberFormat('en-IN', {
  style: 'currency',
  currency: 'INR',
  maximumFractionDigits: 2
});

const formatMoney = (value: number): string => moneyFormatter.format(value);

const formatDate = (dateString: string): string => {
  return new Intl.DateTimeFormat('en-IN', {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
    hour12: true
  }).format(new Date(dateString));
};

/**
 * Modal component for viewing and printing customer sales receipts / bills.
 */
export function ReceiptModal({ sale, onClose }: ReceiptModalProps) {
  if (!sale) return null;

  const handlePrint = () => {
    window.print();
  };

  // Allow Esc key to close and Enter to print
  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        onClose();
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [onClose]);

  const totalItemCount = sale.items.reduce((sum, item) => sum + item.quantity, 0);

  return (
    <div className="modal-backdrop receipt-backdrop" onMouseDown={onClose}>
      <section
        className="modal receipt-modal"
        onMouseDown={(event) => event.stopPropagation()}
      >
        <button
          className="icon-button close no-print"
          onClick={onClose}
          title="Close dialog"
        >
          <X size={20} />
        </button>

        {/* Screen Header Controls (Hidden on Print) */}
        <div className="receipt-modal-header no-print">
          <div className="receipt-success-badge">
            <CheckCircle2 size={18} />
            <span>Sale Recorded Successfully</span>
          </div>
          <h2>Customer Receipt</h2>
          <p>Review the bill summary below or print a physical receipt for the customer.</p>
        </div>

        {/* Printable Thermal Receipt Card */}
        <div className="receipt-paper" id="printable-receipt">
          <div className="receipt-header">
            <div className="receipt-logo">
              <ShoppingBag size={24} />
            </div>
            <h3 className="store-name">COUNTERLY POS</h3>
            <p className="store-tagline">Retail Store & Checkout</p>
            <div className="receipt-badge">TAX INVOICE / BILL</div>
          </div>

          <div className="receipt-divider dashed" />

          <div className="receipt-meta">
            <div className="meta-row">
              <span>Receipt No:</span>
              <strong>#{sale.id.slice(0, 8).toUpperCase()}</strong>
            </div>
            <div className="meta-row">
              <span>Date:</span>
              <span>{formatDate(sale.createdAt)}</span>
            </div>
            <div className="meta-row">
              <span>Cashier:</span>
              <span>{sale.cashierEmail.split('@')[0]}</span>
            </div>
            <div className="meta-row">
              <span>Payment Mode:</span>
              <strong className="payment-badge">{sale.paymentMethod.toUpperCase()}</strong>
            </div>
          </div>

          <div className="receipt-divider dashed" />

          {/* Itemized list */}
          <table className="receipt-table">
            <thead>
              <tr>
                <th className="th-item">ITEM</th>
                <th className="th-qty">QTY</th>
                <th className="th-price">PRICE</th>
                <th className="th-total">TOTAL</th>
              </tr>
            </thead>
            <tbody>
              {sale.items.map((item, index) => (
                <tr key={`${item.productId}-${index}`}>
                  <td className="td-item">
                    <strong>{item.productName}</strong>
                  </td>
                  <td className="td-qty">{item.quantity}</td>
                  <td className="td-price">{formatMoney(item.unitPrice)}</td>
                  <td className="td-total">{formatMoney(item.subtotal)}</td>
                </tr>
              ))}
            </tbody>
          </table>

          <div className="receipt-divider dashed" />

          {/* Financial Totals */}
          <div className="receipt-totals">
            <div className="totals-row">
              <span>Total Items:</span>
              <span>{totalItemCount}</span>
            </div>
            <div className="totals-row">
              <span>Subtotal:</span>
              <span>{formatMoney(sale.totalAmount)}</span>
            </div>
            <div className="totals-row">
              <span>Taxes (GST Incl.):</span>
              <span>₹0.00</span>
            </div>
            <div className="receipt-divider solid" />
            <div className="totals-row grand-total">
              <span>GRAND TOTAL:</span>
              <span>{formatMoney(sale.totalAmount)}</span>
            </div>
          </div>

          <div className="receipt-divider dashed" />

          <div className="receipt-footer">
            <p>Thank you for shopping with us!</p>
            <p className="visit-again">Please visit again</p>
            <small className="system-footer">Powered by Counterly POS</small>
          </div>
        </div>

        {/* Modal Action Footer (Hidden on Print) */}
        <div className="receipt-actions no-print">
          <button className="light-button" onClick={onClose}>
            Done
          </button>
          <button className="primary-button" onClick={handlePrint} autoFocus>
            <Printer size={18} /> Print Bill
          </button>
        </div>
      </section>
    </div>
  );
}
