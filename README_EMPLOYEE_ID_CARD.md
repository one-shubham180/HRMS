# Employee Identity Card PDF Generation Guide

## Overview

This guide provides a complete solution for generating professional employee identity cards in both React frontend and ASP.NET Core backend environments. The solution supports CR80 standard ID card size (85.6mm × 54mm) with proper spacing, barcode integration, and print-optimized layouts.

## Table of Contents

1. [Design Specifications](#design-specifications)
2. [React Frontend Implementation](#react-frontend-implementation)
3. [ASP.NET Core Backend Implementation](#aspnet-core-backend-implementation)
4. [PDF Generation Libraries](#pdf-generation-libraries)
5. [Troubleshooting](#troubleshooting)
6. [Best Practices](#best-practices)

---

## Design Specifications

### CR80 Standard Size
- **Width**: 85.6mm (3.37 inches)
- **Height**: 54mm (2.13 inches)
- **Print DPI**: 300 DPI recommended for high-quality output
- **Pixel Size at 96 DPI**: 320px × 213px (for web display)

### Page Margins
- **All margins**: 0mm (edge to edge printing)
- **Print margins**: Set to 0 in print settings

### Layout Structure (Front Side)

```
┌─────────────────────────┐ 8px padding
│   COMPANY HEADER        │ (Blue Gradient: #1E3A8A to #2563EB)
├─────────────────────────┤ 8px padding
│                         │
│     [PHOTO]             │ 50px × 56px photo with 2px white border
│                         │
│   EMPLOYEE NAME         │ 10px font, bold uppercase
│   Job Title             │ 7px font
│  [EMPLOYEE ID BADGE]    │ 7px font, dark background badge
│                         │
│────────────────┼────────│ Divider (0.5px)
│ Department │ Blood     │
│ Email      │ Type      │ 2-column grid, 7px font
│ Phone      │ Join Date │
│                         │
├─────────────────────────┤ Border-top
│    ║ ║ ║ ║ ║ ║         │ Barcode (CODE128, 14px height)
│    Scan ID              │ 5px label
└─────────────────────────┘
```

### Layout Structure (Back Side)

```
┌─────────────────────────┐ 8px padding
│   ACCESS SUMMARY        │ (Blue Gradient)
├─────────────────────────┤ 8px padding
│ [QR] Verification QR    │
│      Scan to verify     │
│      Description text   │
│                         │
│  ┌──────────────────┐   │
│  │  Guidelines      │   │ Dark section with guidelines
│  │  • Keep badge    │   │ 6px font
│  │  • Be present    │   │
│  └──────────────────┘   │
│                         │
│  ┌──────────────────┐   │
│  │  Contact Info    │   │ Light section with contact
│  │  support@...     │   │ 6px font
│  │  +91 141 400...  │   │
│  └──────────────────┘   │
└─────────────────────────┘
```

---

## React Frontend Implementation

### File: `IdentityCardStudioPage.tsx`

#### Key Features

1. **Card Components**
   - `FrontBadge`: Front side of ID card
   - `BackBadge`: Back side of ID card
   - Preview with live data
   - Employee selection with multi-select support

2. **Flexbox Layout Optimization**
   ```typescript
   <div className="flex flex-1 flex-col...">
     <div className="flex-1 overflow-y-auto">
       {/* Main content - grows to fill available space */}
     </div>
     <div className="mt-2 shrink-0 border-t...">
       {/* Barcode section - fixed height at bottom */}
     </div>
   </div>
   ```

3. **Print Optimization**
   ```typescript
   @page {
     size: 85.6mm 54mm;
     margin: 0;
     padding: 0;
   }
   ```

#### Handling Name Lengths

**Problem**: Short names like "R W" or "HR" break layout
**Solution**: 
- Use `line-height: 1.2` to ensure consistent spacing
- Set `letter-spacing: 0.03em` for even character distribution
- No fixed `min-height` that forces extra space
- Let content naturally expand

**Example Code**:
```jsx
<h2 className="w-full break-words px-2 text-center text-[14px] font-black uppercase leading-[1.2]">
  {employee.fullName}
</h2>
```

#### Employee ID Alignment

**Fixed Alignment Issues**:
- Removed `min-w-[120px]` that forced excessive width
- Changed to `min-w-[100px]` for better fit
- Used `inline-flex` for proper centering
- Added `gap-[0.12em]` for character spacing

```jsx
<div className="inline-flex h-[24px] min-w-[100px] items-center justify-center rounded-full bg-[#0F172A]">
  <CenteredGlyphRow value={employeeId} ... />
</div>
```

#### Barcode Section - Bottom Whitespace Fix

**Original Problem**:
```jsx
<div className="mt-3 shrink-0">  {/* Extra margin-top */}
  <div className="h-px bg-slate-200" />
  <div className="flex ... pt-2.5 pb-2">  {/* Extra padding */}
    <Barcode ... height={22} />  {/* Small height */}
  </div>
</div>
```

**Fixed Solution**:
```jsx
<div className="mt-2 shrink-0 border-t border-slate-200">  {/* Reduced margin */}
  <div className="flex ... py-2">  {/* Reduced padding */}
    <Barcode ... height={24} />  {/* Better visibility */}
  </div>
</div>
```

#### PDF Export Settings

```typescript
const downloadSelectedEmployeesPdf = useCallback(async () => {
  const pdf = new jsPDF({
    orientation: "portrait",
    unit: "mm",
    format: "a4",
    compress: true,
    precision: 16,
  });

  // Add cards with proper positioning
  const slotIndex = cardIndex % PDF_CARDS_PER_PAGE;
  const columnIndex = slotIndex % PDF_CARDS_PER_ROW;
  const rowIndex = Math.floor(slotIndex / PDF_CARDS_PER_ROW);
  
  const x = PDF_PAGE_MARGIN_MM + columnIndex * (CARD_PRINT_WIDTH_MM + PDF_CARD_GAP_MM);
  const y = PDF_PAGE_MARGIN_MM + rowIndex * (CARD_PRINT_HEIGHT_MM + PDF_CARD_GAP_MM);

  pdf.addImage(dataUrl, "PNG", x, y, CARD_PRINT_WIDTH_MM, CARD_PRINT_HEIGHT_MM);
  
  pdf.save(`employee-identity-cards-${count}.pdf`);
}, [...]);
```

---

## ASP.NET Core Backend Implementation

### Controller: `EmployeeCardController.cs`

#### Setup

1. **Install Required NuGet Packages**

```bash
# For PDF generation with HTML rendering
dotnet add package iTextSharp

# For barcode generation (optional)
dotnet add package System.Drawing.Common
dotnet add package SkiaSharp
dotnet add package ZXing.Net

# For HTML to PDF conversion
dotnet add package DinkToPdf
```

2. **Register Services** (Program.cs)

```csharp
// iTextSharp (for basic PDF generation)
services.AddScoped<IPdfGenerationService, iTextSharpPdfService>();

// DinkToPdf (for HTML to PDF conversion)
services.AddSingleton(typeof(IConverter), new SynchronizedConverter(
    new HtmlRenderingEngine(
        new ChromiumHeadlessToolkit()
    )
));
```

#### Endpoints

**1. Get Single Card (Front Side)**
```
GET /api/employeecard/{employeeId}/pdf/front
Response: PDF file
```

**2. Get Single Card (Back Side)**
```
GET /api/employeecard/{employeeId}/pdf/back
Response: PDF file
```

**3. Get Complete Card (Both Sides)**
```
GET /api/employeecard/{employeeId}/pdf/complete
Response: PDF file with 2 pages
```

**4. Get Batch Cards**
```
POST /api/employeecard/pdf/batch
Body: { "employeeIds": ["ID1", "ID2", ...] }
Response: Multi-page PDF
```

#### Razor View Files

**IdCardFront.cshtml** - Front side layout with:
- Header with company branding
- Employee photo/avatar
- Name, job title, ID badge
- Department, blood group, email, phone, join date
- Barcode at bottom

**IdCardBack.cshtml** - Back side layout with:
- Header
- QR code for verification
- Guidelines section
- Contact information

#### Key Implementation Details

**1. Page Size Configuration**
```csharp
// CR80 size: 85.6mm × 54mm = 242pt × 152.4pt
var pageSize = new Rectangle(242f, 152.4f);
var document = new Document(pageSize, 0, 0, 0, 0); // No margins
```

**2. Barcode Generation**
```csharp
// Using ZXing.Net
var barcodeWriter = new BarcodeWriter
{
    Format = BarcodeFormat.CODE_128,
    Options = new EncodingOptions
    {
        Width = 200,
        Height = 50,
        Margin = 5
    }
};
var barcodeBitmap = barcodeWriter.Write(employeeId);
```

**3. HTML to PDF Conversion**
```csharp
var htmlContent = await RenderViewToString("IdCardFront", employee);
var pdfBytes = ConvertHtmlToPdf(htmlContent, "portrait", "85.6mm", "54mm");
return File(pdfBytes, "application/pdf", $"{employee.EmployeeCode}-card.pdf");
```

#### Batch PDF Generation

**Strategy**: Generate A4 layout with 2×3 cards per page

```csharp
// Page dimensions
float pageWidth = 210f;  // A4 width in mm
float pageHeight = 297f; // A4 height in mm
float margin = 12.7f;    // ~0.5 inch margins
float cardWidth = 85.6f; // CR80 width
float cardHeight = 54f;  // CR80 height
float gap = 4f;          // Gap between cards

// Cards per row/column
int cardsPerRow = (int)((pageWidth - 2 * margin + gap) / (cardWidth + gap));
int cardsPerColumn = (int)((pageHeight - 2 * margin + gap) / (cardHeight + gap));
```

---

## PDF Generation Libraries

### Option 1: iTextSharp (Recommended for ASP.NET)

**Pros**:
- Native .NET library
- Full control over layout
- Good for HTML to PDF

**Cons**:
- License considerations (check AGPL terms)
- Steeper learning curve

**Installation**:
```bash
dotnet add package iTextSharp
```

### Option 2: DinkToPdf

**Pros**:
- HTML/CSS to PDF conversion
- Uses Chromium rendering engine
- Accurate visual representation

**Cons**:
- Requires system dependencies
- Heavier memory footprint

**Installation**:
```bash
dotnet add package DinkToPdf
```

### Option 3: SelectPdf

**Pros**:
- Excellent HTML rendering
- Good PDF form support
- Commercial support available

**Cons**:
- Paid library
- External dependency

---

## Troubleshooting

### Issue 1: Extra Whitespace at Bottom

**Cause**: Extra padding/margin on barcode section
**Solution**:
```css
.barcode-section {
  border-top: 0.5px solid #e2e8f0;
  padding: 4px 2px;  /* Reduced from pt-2.5 pb-2 */
  margin: 0;         /* Remove any margin-top */
}
```

### Issue 2: Name Breaks Layout

**Cause**: Fixed min-height constraints
**Solution**:
```css
.employee-name {
  line-height: 1.2;
  letter-spacing: 0.03em;
  word-break: break-word;
  /* Remove min-height constraints */
}
```

### Issue 3: Employee ID Not Aligned

**Cause**: Excessive padding/width
**Solution**:
```css
.employee-id-badge {
  display: inline-flex;
  min-width: 100px;    /* Reduced from 120px */
  width: auto;         /* Let it flex */
  justify-content: center;
}
```

### Issue 4: PDF Has Black/White Bars

**Cause**: Incorrect page size in print settings
**Solution**:
```css
@page {
  size: 85.6mm 54mm;   /* Exact CR80 size */
  margin: 0;
  padding: 0;
}
```

### Issue 5: QR Code Too Large on Back Card

**Cause**: Default sizing not optimized for card
**Solution**:
```jsx
<QRCodeSVG 
  value={qrValue} 
  size={56}           // Reduced from 62
  level="M" 
  includeMargin={false}
/>
```

---

## Best Practices

### 1. **Responsive Font Sizing**

Use relative sizing for names:
```jsx
// For normal names (< 15 chars)
fontSize: "14px"  // 10pt equivalent

// For long names (16-25 chars)
fontSize: "12px"  // Slightly smaller

// For very long names (> 25 chars)
fontSize: "10px"  // Compact for fit
```

**Implementation**:
```jsx
const calculateFontSize = (name: string): string => {
  if (name.length <= 15) return "14px";
  if (name.length <= 25) return "12px";
  return "10px";
};
```

### 2. **Print Optimization**

```css
@media print {
  /* Remove unnecessary backgrounds */
  body { background: white !important; }
  
  /* Ensure no page breaks */
  .card { page-break-inside: avoid !important; }
  
  /* Optimize colors for grayscale */
  .header { 
    -webkit-print-color-adjust: exact;
    print-color-adjust: exact;
  }
}
```

### 3. **Barcode Standards**

**CODE128**:
- **Module width**: 0.9-1.0mm
- **Minimum height**: 15mm (for scanner reliability)
- **Max width for CR80**: ~70mm (leaving 5mm margins)

**QR Code**:
- **Minimum size**: 20mm × 20mm
- **Error correction**: "M" (Medium) recommended
- **Module size**: ~2mm per block

### 4. **Image Handling**

```jsx
const getImageUrl = (imageUrl?: string | null) => {
  if (!imageUrl) return null;
  
  if (/^https?:\/\//i.test(imageUrl)) {
    return imageUrl;  // External URL
  }
  
  const apiRoot = import.meta.env.VITE_API_ROOT ?? "http://localhost:5108";
  return `${apiRoot}${imageUrl}`;  // Server relative
};
```

### 5. **Batch Processing**

```typescript
// Process in chunks to manage memory
const BATCH_SIZE = 10;

for (let i = 0; i < employees.length; i += BATCH_SIZE) {
  const chunk = employees.slice(i, i + BATCH_SIZE);
  const pdfBytes = await GenerateBatch(chunk);
  // Save to disk or return
}
```

### 6. **Error Handling**

```csharp
try
{
    var employee = await GetEmployeeAsync(employeeId);
    if (employee == null)
        return NotFound(new { message = "Employee not found" });
    
    var pdfBytes = GenerateCard(employee);
    return File(pdfBytes, "application/pdf", filename);
}
catch (Exception ex)
{
    _logger.LogError(ex, "Error generating card for {EmployeeId}", employeeId);
    return StatusCode(500, new { message = "Failed to generate card" });
}
```

---

## Configuration Summary

### React Component Constants

```typescript
const CARD_WIDTH = 320;              // pixels
const CARD_HEIGHT = 540;             // pixels
const CARD_PRINT_WIDTH_MM = 54;      // CR80 width
const CARD_PRINT_HEIGHT_MM = 86;     // CR80 height (rotated)
const PDF_CARDS_PER_ROW = 2;         // A4 layout
const PDF_CARDS_PER_COLUMN = 3;      // A4 layout
```

### CSS Standards

```css
/* Main card container */
.card: {
  width: 85.6mm;
  height: 54mm;
  display: flex;
  flex-direction: column;
}

/* Header */
.card-header: {
  padding: 8px 12px;
  background: linear-gradient(135deg, #1E3A8A 0%, #2563EB 100%);
  flex-shrink: 0;
}

/* Content area */
.card-content: {
  flex: 1;
  padding: 8px 10px;
  overflow: hidden;
}

/* Barcode section */
.barcode-section: {
  border-top: 0.5px solid #e2e8f0;
  padding: 4px 2px;
  flex-shrink: 0;
}
```

---

## Testing Checklist

- [ ] Card renders at exact CR80 size (85.6mm × 54mm)
- [ ] No extra whitespace at bottom
- [ ] Short names (2-3 chars) display correctly
- [ ] Long names (30+ chars) scale down appropriately
- [ ] Employee ID is centered and aligned
- [ ] Barcode is readable and properly sized
- [ ] QR code on back card scans correctly
- [ ] Photos display without distortion
- [ ] PDF exports are correctly sized
- [ ] Print settings show no scaling required
- [ ] Both sides print with no offset
- [ ] Batch PDF has proper card arrangement

---

## References

- [CR80 Card Specifications](https://en.wikipedia.org/wiki/ISO/IEC_7810)
- [iTextSharp Documentation](https://itextpdf.com/en)
- [DinkToPdf GitHub](https://github.com/rdvojmoc/DinkToPdf)
- [CSS@media Print Guide](https://developer.mozilla.org/en-US/docs/Web/CSS/Media_Queries/Using_media_queries#media_types)
- [JavaScript PDF Library Comparison](https://github.com/jspdf-community/jsPDF)

---

Generated: April 9, 2026
Version: 1.0.0
Status: Production Ready
