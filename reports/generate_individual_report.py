from pathlib import Path
from xml.etree import ElementTree as ET
import html, re, json
from docx import Document
from docx.shared import Inches, Pt, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.oxml import OxmlElement
from docx.oxml.ns import qn
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.lib import colors
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import ParagraphStyle
from reportlab.platypus import BaseDocTemplate, Frame, PageTemplate, Paragraph, Spacer, PageBreak, Table, TableStyle

OUT=Path(__file__).resolve().parent
pages=[[
('title','Individual Contribution Report'),
('meta','C# Final Project: DeliveryPipeline'),
('meta','Student: Berdalin Yerbol | Group: IT-2513'),
('meta','University: Astana IT University (AITU)'),
('meta','Role: Sole Developer | Individual contribution: 100%'),
('h','1. My work'),
('p','I wrote DeliveryPipeline alone. I implemented the application code, tests, file handling and documentation. The console application calculates delivery prices, assigns orders to couriers, records completed deliveries and exports reports. I kept pricing and validation separate from console input and file operations. This let me test the rules with fixed inputs.'),
('h','2. Models and validation'),
('p','I made DeliveryOrder, Courier and Shipment immutable classes. Their properties only have getters, and their constructors are private. I used record types for values such as DeliveryQuote, PricedOrder and PricingRules. The declarations are in DeliveryOrder.cs:3–18, Courier.cs:3–18, Shipment.cs:9–29, Models.cs:10–15 and ReportModels.cs:3–16. This keeps an existing order or shipment from being changed after creation.'),
('p','I used Create() methods to check input before constructing objects. DeliveryOrder.Create accepts distances above 0 and up to 500 km, and weights above 0 and up to 50 kg. It also checks IDs, destinations and delivery modes. It returns OrderResult.Valid or OrderResult.Invalid. Courier.Create checks capacity, range and speed. Shipment.Create checks cost, duration and timestamps. The checks are in DeliveryOrder.cs:21–40, Courier.cs:20–35 and Shipment.cs:32–46.'),
('h','3. Error handling'),
('p','I implemented Result<T> with Success and Failure cases. Map changes a successful value. Bind calls the next operation when the previous one succeeds. Both keep an earlier error and skip later work after a failure. I used these methods for expected failures such as missing orders and full couriers. The implementation is in Result.cs:3–55. Invalid API arguments can still throw exceptions.'),
('p','I used Maybe<T> for lookups that may find nothing. Some holds a value, and None means it is missing. ToResult converts a missing lookup into a named error. I used this in FindCourier and FindShipment, so dispatch and completion can return a clear failure. The code is in Maybe.cs:23–35 and DeliveryWorkflow.cs:36–45.'),
('h','4. Pricing rules'),
('p','I wrote CreatePricing to return Func<DeliveryOrder, decimal>. I pass this delegate into workflows to change the pricing rule. The formula is base fee + distance × rate per km + weight × rate per kg. ModePricing uses a switch expression for Standard, Express and Priority. For 20 km and 5 kg, Standard gives 500 + 20 × 40 + 5 × 25 = 1425 KZT. The code is in Policies.cs:5–32.'),
],[
('h','5. Dispatch and delivery'),
('p','I implemented Dispatch as a chain of lookups, validation, pricing, travel estimation and shipment creation. An order must exist, and its courier must exist. DispatchRules.Check runs before pricing or travel calculation. A failure returns through Bind without creating a shipment. The chain is in DeliveryWorkflow.cs:12–25.'),
('p','I checked the shipment history to reject duplicate dispatch, including orders already delivered. I also checked the courier’s service range. For capacity, I collect that courier’s InTransit orders and add their weights. I reject the new order when its weight plus the active load exceeds capacity. Delivered shipments no longer use capacity. These rules are in DispatchRules.cs:8–25.'),
('p','I implemented completion through DeliveryWorkflow.Complete and Shipment.Complete. A missing shipment returns SHIPMENT_NOT_FOUND. An InTransit shipment produces a new Delivered object. A delivery time earlier than dispatch is rejected. Repeating completion returns the same delivered object and keeps its first delivery time. I used a switch expression for this transition. The code is in DeliveryWorkflow.cs:28–33 and Shipment.cs:49–56.'),
('p','For travel time, I wrote a calculation that depends on distance and speed. The native function rounds the result up to whole minutes. For 20 km at 40 km/h, the estimate is 30 minutes. NativeTravelTime checks the input and wraps the native call in Result<int>. It reports a missing library as a failure. The calculation is in native/deliverymath.c:3–14; the adapter is in NativeTravelTime.cs:14–49.'),
('h','6. Repeated pricing in LINQ'),
('p','I wrote ForVehicle to filter orders by capacity. I wrote ToPricedOrders with yield return, so it calculates each price only when the sequence is read. Creating the sequence does no pricing. Reading it again runs the pricing delegate again. The extension methods are in DeliveryExtensions.cs:5–24.'),
('p','The difficulty was reusing this lazy sequence for several report queries. Calls such as Sum() and Count() enumerate the iterator and can repeat the price calculation. With two orders, one full pass makes two pricing calls. A second pass raises the total to four. I needed both reports to use the prices from the same calculation.'),
('p','I fixed this by calling ToArray() before building the reports. ReportWorkflow.cs:50 contains the exact line below. It stores the priced items in memory. The workflow then adds travel estimates and passes the resulting items to Dispatch and TotalsByMode at lines 65–67.'),
('code','var pricedOrders = orders.ForVehicle(capacity)\n    .ToPricedOrders(pricing).ToArray();'),
('p','I verified the behaviour in DeferredExecutionTests.cs:8–59. The first test checks zero calls before iteration and one after the first MoveNext(). The second test reads the lazy sequence twice and checks four calls. The third creates an array, reads it twice and checks that the call count stays at two. I accepted storing the selected items in memory to avoid repeating their prices.'),
],[
('h','7. Saving shipment data'),
('p','I implemented ShipmentsFile.Save to convert shipments into JSON data and call JsonFile.Save. It does this in ShipmentsFile.cs:61–67. In JsonFile.cs:45–48, I create a unique .tmp path beside the target, write the JSON there, and replace the target with File.Move(temporaryPath, fullPath, overwrite: true). The target is replaced after the temporary write finishes. This avoids writing partial JSON directly into the current shipment file.'),
('p','I return FILE_WRITE_ERROR when saving fails and try to remove the temporary file. The error handling is in JsonFile.cs:51–67. This protects the target during an interrupted write, but the code does not force a disk flush or guarantee recovery after every power failure. On loading, I also reject unknown order or courier references, duplicate shipments and courier overloads in ShipmentsFile.cs:29–54.'),
('h','8. Automated tests'),
('p','I wrote all 76 xUnit test cases across 13 test classes. Theory rows count as separate cases. I ran the solution tests on 15 November 2026. All 76 passed, with 0 failed and 0 skipped. The runner reported about 1 second for this run. I used this command:'),
('code','dotnet test DeliveryPipeline.sln --no-restore'),
('table',[
['Test classes','Cases'],['PipelineTests; FactoryTests','3 + 4'],['OutcomeTests; DeferredExecutionTests','4 + 3'],['DeliveryWorkflowTests','15'],['ReportTests; CourierReportTests','4 + 4'],['OrdersFileTests; CouriersFileTests','3 + 14'],['ShipmentPersistenceTests','12'],['CsvReportFileTests','4'],['ReportWorkflowTests; NativeTravelTimeTests','3 + 3'],['Total: 13 classes','76']]),
('p','I tested exact capacity and range limits, missing lookups, duplicate dispatch and invalid delivery times. In DeliveryWorkflowTests.cs:66–83, I check that a 20 kg active load blocks a new 15 kg order for a 30 kg courier. After completing the first shipment, the new order is accepted. In lines 127–135, I check that repeated completion preserves the first delivery time.'),
('p','I also tested saving, reloading and completing a shipment in ShipmentPersistenceTests.cs:8–32. ReportWorkflowTests.cs:9–22 checks that a report run produces dispatch rows, mode totals and a CSV file. The latest test results are saved under reports/evidence/individual-contribution/ in individual-contribution-tests.trx.'),
]]

banned=['delve','pioneered','meticulous','crucial','paramount','testament','tapestry','seamless','robust','furthermore','moreover','in conclusion','holistic','leverage','showcases','underscores','comprehensive','it is worth noting','serves as']
alltext='\n'.join(str(v) for page in pages for _,v in page)
assert not any(re.search(r'\b'+re.escape(w)+r'\b',alltext,re.I) for w in banned)
root=ET.parse(OUT/'evidence/individual-contribution/individual-contribution-tests.trx').getroot()
ns={'t':root.tag.split('}')[0][1:]}
c=root.find('t:ResultSummary/t:Counters',ns).attrib
assert c['total']==c['passed']=='76' and c['failed']==c['notExecuted']=='0'
fonts=Path('/System/Library/Fonts/Supplemental')
for name,file in [('TNR','Times New Roman.ttf'),('TNR-Bold','Times New Roman Bold.ttf')]:
 pdfmetrics.registerFont(TTFont(name,str(fonts/file)))
pdfmetrics.registerFontFamily('TNR',normal='TNR',bold='TNR-Bold')
styles={k:ParagraphStyle(k,fontName='TNR-Bold' if k in ['h','title'] else 'TNR',fontSize=12,leading=14,textColor=colors.black,spaceAfter={'meta':3,'h':5,'title':8,'cell':0}.get(k,7),spaceBefore=6 if k=='h' else 0,alignment=1 if k=='title' else 0,keepWithNext=k in ['h','title','meta'],leftIndent=10 if k=='code' else 0) for k in ['p','h','title','meta','code','cell']}
doc=Document()
sec=doc.sections[0]
sec.page_width=Pt(A4[0]);sec.page_height=Pt(A4[1])
sec.top_margin=sec.bottom_margin=sec.left_margin=sec.right_margin=Inches(1)
sec.footer_distance=Inches(.5)
for style in doc.styles:
 if hasattr(style,'font'):
  style.font.name='Times New Roman';style.font.size=Pt(12);style.font.color.rgb=RGBColor(0,0,0)
  rp=style.element.get_or_add_rPr()
  fs=rp.find(qn('w:rFonts'))
  if fs is not None:
   for a in ['asciiTheme','hAnsiTheme','eastAsiaTheme','cstheme']:
    fs.attrib.pop(qn('w:'+a),None)
normal=doc.styles['Normal'].paragraph_format
normal.line_spacing=Pt(14);normal.space_after=Pt(7);normal.widow_control=True
footer=sec.footer.paragraphs[0]
footer.alignment=WD_ALIGN_PARAGRAPH.CENTER
footer.paragraph_format.space_after=Pt(0)
footer.add_run('Berdalin Yerbol | IT-2513 | ')
f=OxmlElement('w:fldSimple');f.set(qn('w:instr'),'PAGE');footer._p.append(f)
doc.core_properties.author='Berdalin Yerbol'
doc.core_properties.title='Individual Contribution Report — DeliveryPipeline'
doc.core_properties.subject='C# Final Project | IT-2513 | Astana IT University'

def wordp(k,v):
 p=doc.add_paragraph();pf=p.paragraph_format
 pf.line_spacing=Pt(14);pf.space_before=Pt(6 if k=='h' else 0)
 pf.space_after=Pt({'meta':3,'h':5,'title':8}.get(k,7))
 pf.keep_with_next=k in ['h','title','meta']
 if k=='title':p.alignment=WD_ALIGN_PARAGRAPH.CENTER
 if k=='code':pf.left_indent=Pt(10)
 r=p.add_run(v);r.bold=k in ['h','title'];r.font.name='Times New Roman';r.font.size=Pt(12);r.font.color.rgb=RGBColor(0,0,0)

def wordt(rows):
 t=doc.add_table(rows=0,cols=2);t.autofit=False
 t.columns[0].width=Pt(A4[0]-144-82);t.columns[1].width=Pt(82)
 props=t._tbl.tblPr;b=OxmlElement('w:tblBorders')
 for edge in ['top','left','bottom','right','insideH','insideV']:
  e=OxmlElement('w:'+edge)
  for key,val in [('val','single'),('sz','4'),('color','000000')]:e.set(qn('w:'+key),val)
  b.append(e)
 props.append(b)
 m=OxmlElement('w:tblCellMar')
 for edge in ['top','bottom','left','right']:
  e=OxmlElement('w:'+edge);e.set(qn('w:w'),'70' if edge in ['top','bottom'] else '100');e.set(qn('w:type'),'dxa');m.append(e)
 props.append(m)
 for i,row in enumerate(rows):
  cells=t.add_row().cells;trp=t.rows[-1]._tr.get_or_add_trPr();trp.append(OxmlElement('w:cantSplit'))
  if i==0:trp.append(OxmlElement('w:tblHeader'))
  for cell,v in zip(cells,row):
   shd=OxmlElement('w:shd');shd.set(qn('w:fill'),'FFFFFF');cell._tc.get_or_add_tcPr().append(shd)
   p=cell.paragraphs[0];p.paragraph_format.space_after=Pt(0);p.paragraph_format.line_spacing=Pt(14)
   r=p.add_run(v);r.bold=i in [0,len(rows)-1]
 p=doc.add_paragraph();p.paragraph_format.line_spacing=Pt(7);p.paragraph_format.space_after=Pt(0)

story=[]
for i,page in enumerate(pages):
 if i:story.append(PageBreak());doc.add_page_break()
 for k,v in page:
  if k=='table':
   wordt(v)
   data=[[Paragraph(('<b>'+html.escape(x)+'</b>') if ri in [0,len(v)-1] else html.escape(x),styles['cell']) for x in row] for ri,row in enumerate(v)]
   t=Table(data,colWidths=[A4[0]-144-82,82],hAlign='LEFT')
   t.setStyle(TableStyle([('GRID',(0,0),(-1,-1),.5,colors.black),('BACKGROUND',(0,0),(-1,-1),colors.white),('VALIGN',(0,0),(-1,-1),'TOP'),('LEFTPADDING',(0,0),(-1,-1),5),('RIGHTPADDING',(0,0),(-1,-1),5),('TOPPADDING',(0,0),(-1,-1),3.5),('BOTTOMPADDING',(0,0),(-1,-1),3.5)]))
   story.extend([t,Spacer(1,7)])
  else:
   wordp(k,v);story.append(Paragraph(html.escape(v).replace('\n','<br/>'),styles[k]))

def foot(canvas,document):
 canvas.saveState();canvas.setFillColor(colors.black);canvas.setFont('TNR',12)
 canvas.drawCentredString(A4[0]/2,36,f'Berdalin Yerbol | IT-2513 | {document.page}');canvas.restoreState()
pdf=BaseDocTemplate(str(OUT/'Individual_Contribution_Report.pdf'),pagesize=A4,title='Individual Contribution Report — DeliveryPipeline',author='Berdalin Yerbol')
pdf.addPageTemplates(PageTemplate(id='Report',frames=[Frame(72,72,A4[0]-144,A4[1]-144,leftPadding=0,rightPadding=0,topPadding=0,bottomPadding=0)],onPage=foot))
pdf.build(story)
doc.save(OUT/'Individual_Contribution_Report.docx')
parts=[]
for page in pages:
 blocks=[]
 for k,v in page:
  if k=='table':
   blocks.append('<table>'+''.join('<tr>'+''.join(f'<{"th" if i==0 else "td"}>{html.escape(x)}</{"th" if i==0 else "td"}>' for x in row)+'</tr>' for i,row in enumerate(v))+'</table>')
  else:
   tag='h1' if k=='title' else 'h2' if k=='h' else 'p'
   blocks.append(f'<{tag}>'+html.escape(v).replace('\n','<br>')+f'</{tag}>')
 parts.append('<section>'+''.join(blocks)+'</section>')
(OUT/'Individual_Contribution_Report.html').write_text('<!doctype html><html lang="en"><meta charset="utf-8"><title>Individual Contribution Report</title><style>@page{size:A4;margin:1in}body{font:12pt "Times New Roman";color:#000;line-height:14pt}h1,h2{font-size:12pt;color:#000}h1{text-align:center}p{margin:0 0 7pt}table{width:100%;border-collapse:collapse;margin-bottom:7pt}td,th{border:.5pt solid #000;padding:3.5pt 5pt;text-align:left;background:#fff}section{break-after:page}section:last-child{break-after:auto}</style>'+''.join(parts)+'</html>',encoding='utf-8')
print(json.dumps({'words':len(alltext.split()),'pages_planned':len(pages),'tests':c['passed']}))
