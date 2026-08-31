import { useState } from 'react';
import { Copy, Layers, Printer, ScanBarcode, Sparkles, Tag, X } from 'lucide-react';
import type { Product } from '../api/types';

interface BarcodeLabelModalProps {
  product: Product | null;
  onClose: () => void;
}

/**
 * Standard Code 128 Barcode Pattern Definitions (Table B).
 */
const CODE128_PATTERNS: string[] = [
  '212222', '222122', '222221', '121223', '121322', '131222', '122213', '122312', '132212', '221213',
  '221312', '231212', '112232', '122132', '122231', '113222', '123122', '123221', '223211', '221132',
  '221231', '213212', '223112', '312131', '311222', '321122', '321221', '312212', '322112', '322211',
  '212123', '212321', '232121', '111323', '131123', '131321', '112313', '132113', '132311', '211313',
  '231113', '231311', '112133', '112331', '132131', '113123', '113321', '133121', '313121', '211331',
  '231131', '213113', '213311', '213131', '311123', '311321', '331121', '312113', '312311', '332111',
  '314111', '221411', '431111', '111224', '111422', '121124', '121421', '141122', '141221', '112214',
  '112412', '122114', '122411', '142112', '142211', '241211', '221114', '413111', '241112', '134111',
  '111242', '121142', '121241', '114212', '124112', '124211', '411212', '421112', '421211', '212141',
  '214121', '412121', '111143', '111341', '131141', '114113', '114311', '411113', '411311', '113141',
  '114131', '311141', '411131', '211412', '211214', '211232', '2331112'
];

/**
 * Generates an SVG string representation of a standard Code 128B barcode.
 */
function renderCode128Svg(text: string): { svgBars: { x: number; width: number }[]; totalWidth: number } {
  const cleanText = text.trim() || '000000';
  const START_B = 104;
  const STOP = 106;

  const codes: number[] = [START_B];
  let checksum = START_B;

  for (let i = 0; i < cleanText.length; i++) {
    const code = cleanText.charCodeAt(i) - 32;
    const validCode = code >= 0 && code <= 95 ? code : 0;
    codes.push(validCode);
    checksum += validCode * (i + 1);
  }

  const checkDigit = checksum % 103;
  codes.push(checkDigit);
  codes.push(STOP);

  let patternStr = '';
  for (const c of codes) {
    patternStr += CODE128_PATTERNS[c] || '212222';
  }

  const bars: { x: number; width: number }[] = [];
  let currentX = 10; // Quiet zone padding
  let isBar = true;

  for (let i = 0; i < patternStr.length; i++) {
    const width = parseInt(patternStr[i], 10);
    if (isBar) {
      bars.push({ x: currentX, width });
    }
    currentX += width;
    isBar = !isBar;
  }

  return { svgBars: bars, totalWidth: currentX + 10 };
}

/**
 * Currency formatter for Indian Rupees (INR).
 */
const formatMoney = (value: number): string =>
  new Intl.NumberFormat('en-IN', {
    style: 'currency',
    currency: 'INR',
    maximumFractionDigits: 2
  }).format(value);

/**
 * Vector SVG component for drawing sharp, scannable Code 128 barcodes.
 */
function BarcodeSvg({ value, height = 40 }: { value: string; height?: number }) {
  const { svgBars, totalWidth } = renderCode128Svg(value);

  return (
    <svg
      viewBox={`0 0 ${totalWidth} ${height}`}
      className="barcode-vector-svg"
      preserveAspectRatio="none"
    >
      <rect width="100%" height="100%" fill="#ffffff" />
      {svgBars.map((bar, idx) => (
        <rect
          key={idx}
          x={bar.x}
          y="0"
          width={bar.width}
          height={height}
          fill="#000000"
        />
      ))}
    </svg>
  );
}

/**
 * Modal dialog for previewing and printing scannable barcode sticker labels.
 */
export function BarcodeLabelModal({ product, onClose }: BarcodeLabelModalProps) {
  if (!product) return null;

  const [quantity, setQuantity] = useState<number>(Math.max(1, Math.min(product.stockQty || 1, 100)));
  const [storeName, setStoreName] = useState('COUNTERLY STORE');
  const [layout, setLayout] = useState<'roll' | 'sheet'>('roll');

  const handlePrint = () => {
    window.print();
  };

  const stickers = Array.from({ length: Math.max(1, quantity) });

  return (
    <div className="modal-backdrop label-modal-backdrop" onMouseDown={onClose}>
      <section
        className="modal label-modal"
        onMouseDown={(e) => e.stopPropagation()}
      >
        <button className="icon-button close no-print" onClick={onClose} title="Close">
          <X size={20} />
        </button>

        {/* Configuration Toolbar (Hidden on Print) */}
        <div className="label-modal-header no-print">
          <div className="label-badge">
            <Tag size={16} />
            <span>Barcode Sticker Generator</span>
          </div>
          <h2>Print Barcode Labels</h2>
          <p>Generate adhesive sticker tags ready for your barcode label printer.</p>
        </div>

        <div className="label-controls no-print">
          <div className="form-row">
            <label>
              Number of Stickers to Print
              <div className="quantity-preset-group">
                <input
                  type="number"
                  min="1"
                  max="500"
                  value={quantity}
                  onChange={(e) => setQuantity(Math.max(1, Number(e.target.value) || 1))}
                />
                <button
                  type="button"
                  className="preset-btn"
                  onClick={() => setQuantity(1)}
                  title="Print 1 sticker"
                >
                  1
                </button>
                <button
                  type="button"
                  className="preset-btn"
                  onClick={() => setQuantity(10)}
                  title="Print 10 stickers"
                >
                  10
                </button>
                {product.stockQty > 0 && (
                  <button
                    type="button"
                    className="preset-btn stock-preset"
                    onClick={() => setQuantity(product.stockQty)}
                    title="Print matching current stock count"
                  >
                    Stock ({product.stockQty})
                  </button>
                )}
              </div>
            </label>

            <label>
              Store / Header Name
              <input
                value={storeName}
                onChange={(e) => setStoreName(e.target.value)}
                placeholder="e.g. My Retail Store"
              />
            </label>
          </div>

          <div className="layout-selector">
            <span className="layout-title">Printer Output Mode:</span>
            <div className="layout-options">
              <button
                type="button"
                className={`layout-btn ${layout === 'roll' ? 'active' : ''}`}
                onClick={() => setLayout('roll')}
              >
                <Layers size={16} /> Continuous Thermal Roll (50mm × 25mm)
              </button>
              <button
                type="button"
                className={`layout-btn ${layout === 'sheet' ? 'active' : ''}`}
                onClick={() => setLayout('sheet')}
              >
                <Copy size={16} /> A4 Sticker Sheet Grid
              </button>
            </div>
          </div>
        </div>

        {/* Printable Labels Canvas */}
        <div className="label-preview-container">
          <div className="label-preview-banner no-print">
            <ScanBarcode size={16} />
            <span>
              Previewing {quantity} sticker{quantity > 1 ? 's' : ''} for "{product.name}"
            </span>
          </div>

          <div
            id="printable-barcode-sheet"
            className={`stickers-grid ${layout === 'sheet' ? 'a4-sheet' : 'roll-sheet'}`}
          >
            {stickers.map((_, index) => (
              <div className="barcode-sticker" key={index}>
                <div className="sticker-header">
                  <span className="sticker-store">{storeName}</span>
                </div>
                <strong className="sticker-title" title={product.name}>
                  {product.name}
                </strong>
                <div className="sticker-barcode-wrap">
                  <BarcodeSvg value={product.sku} height={32} />
                  <span className="sticker-sku-digits">{product.sku}</span>
                </div>
                <div className="sticker-footer">
                  <span className="sticker-mrp-label">MRP:</span>
                  <strong className="sticker-price">{formatMoney(product.price)}</strong>
                  <span className="sticker-tax-note">(Incl. of all taxes)</span>
                </div>
              </div>
            ))}
          </div>
        </div>

        {/* Action Controls (Hidden on Print) */}
        <div className="label-modal-footer no-print">
          <button className="light-button" onClick={onClose}>
            Cancel
          </button>
          <button className="primary-button" onClick={handlePrint} autoFocus>
            <Printer size={18} /> Print {quantity} Sticker{quantity > 1 ? 's' : ''}
          </button>
        </div>
      </section>
    </div>
  );
}
