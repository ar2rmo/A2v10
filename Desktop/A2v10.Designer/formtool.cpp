
#include "stdafx.h"

#include "formitem.h"
#include "formtool.h"
#include "a2formview.h"
#include "a2formdoc.h"
#include "recttracker.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#endif


// static 
CList<CFormTool*> CFormTool::s_toolsList;
// static 
CFormItem::Shape CFormTool::s_currentShape = CFormItem::_pointer;

UINT CFormTool::s_currentId = ID_TOOLBOX_POINTER;

CFormTool::CFormTool(CFormItem::Shape shape, UINT nID)
	: m_eShape(shape), m_nID(nID)
{
	s_toolsList.AddTail(this);
}

static CFormSelectTool selectTool;
static CFormTool buttonTool  (CFormItem::_button, ID_TOOLBOX_BUTTON);
static CFormTool textBoxTool (CFormItem::_textbox, ID_TOOLBOX_TEXTBOX);
static CFormTool checkBoxTool(CFormItem::_checkbox, ID_TOOLBOX_CHECK);
static CFormTool radioTool   (CFormItem::_radio, ID_TOOLBOX_RADIO);
static CFormTool comboBoxTool(CFormItem::_combobox, ID_TOOLBOX_COMBOBOX);
static CFormTool dataGridTool(CFormItem::_datagrid, ID_TOOLBOX_DATAGRID);

static CFormTool canvasTool(CFormItem::_canvas, ID_TOOLBOX_CANVAS);
static CFormTool gridTool(CFormItem::_grid, ID_TOOLBOX_GRID);

// static 
void CFormTool::SetShape(UINT nID)
{
	POSITION pos = s_toolsList.GetHeadPosition();
	while (pos != NULL) {
		CFormTool* pTool = s_toolsList.GetNext(pos);
		if (pTool->m_nID == nID) {
			s_currentId = pTool->m_nID;
			s_currentShape = pTool->m_eShape;
			return;
		}
	}
	s_currentId = 0;
	s_currentShape = CFormItem::_undefined;
}

// static 
bool CFormTool::IsShape(UINT nID)
{
	return nID == s_currentId;
}

// static 
CFormTool* CFormTool::FindTool()
{
	POSITION pos = s_toolsList.GetHeadPosition();
	while (pos != NULL) {
		CFormTool* pTool = s_toolsList.GetNext(pos);
		if (pTool->m_eShape == s_currentShape)
			return pTool;
	}
	return NULL;
}

// static
CFormItem* CFormTool::CreateItem(CFormItem::Shape shape, const CRect& rect, CFormItem* pParent)
{
	CFormItem* pItem = CFormItem::CreateElement(shape, pParent);
	if (!pItem) {
		AfxMessageBox(L"Can't create element");
		return nullptr;
	}
	CRect newRect(rect);
	pItem->DoAdjustTrackRect(newRect, CPoint(0, 0));
	pItem->MoveTo(newRect);
	return pItem;
}

// virtual 
void CFormTool::OnLButtonDown(CA2FormView* pView, UINT nFlags, const CPoint& point)
{
	ASSERT_VALID(this);
	ASSERT_VALID(pView);
	CRectTrackerEx tracker;
	if (!tracker.TrackRubberBand(pView, point))
		return;
	// Mouse has moved
	CRect nr(tracker.m_rect);
	CPoint local(point);
	nr.NormalizeRect();

	auto pDoc = pView->GetDocument();

	CClientDC dc(pView);
	pView->OnPrepareDC(&dc, nullptr);
	dc.DPtoLP(&local); // ClientToDoc
	dc.DPtoLP(nr);	   // ClientToDoc

	auto pParent = pDoc->ObjectAt(local);
	if (!pParent) {
		OnCancel();
		return;
	}

	//if (pParent)
		//pParent = pParent->GetCreateTarget();
	// Convert to doc coords and snap to grid if needed
	pView->PrepareNewRect(nr);
	// Create new object and select it 
	auto pNewItem = CreateItem(m_eShape, nr, pParent);
	pView->SelectItem(pNewItem);

	if ((nFlags & MK_SHIFT) == 0)
		OnCancel();
}

// virtual 
void CFormTool::OnLButtonDblClk(CA2FormView* pView, UINT nFlags, const CPoint& point)
{

}

// static
void CFormTool::OnCancel()
{
	CFormTool::SetShape(ID_TOOLBOX_POINTER);
}


CFormSelectTool::CFormSelectTool()
	: CFormTool(CFormItem::_pointer, ID_TOOLBOX_POINTER)
{
}

// virtual 
void CFormSelectTool::OnLButtonDown(CA2FormView* pView, UINT nFlags, const CPoint& point)
{
	bool bShift = (nFlags & MK_SHIFT) != 0;
	CA2FormDocument* pDoc = pView->GetDocument();
	ATLASSERT(pDoc);
	bool bLocked = pDoc->IsLocked();

	CPoint local(point);
	CClientDC dc(pView);
	pView->OnPrepareDC(&dc, nullptr);
	dc.DPtoLP(&local); // ClientToDoc

	CFormItem* pItem = pDoc->ObjectAt(local);
	int cnt = pView->m_selection.GetCount();
	if (cnt == 0) {

	}
	else if (cnt > 1) {
		if (!bLocked && MoveObjects(pView, point))
			return;
	}
	else if (cnt == 1) {
		if (!bLocked && HandleOneObject(pView, point))
			return; // already done
	}
	if (pItem)
	{
		pView->SelectItem(pItem, bShift);
		if (!bShift) {
			if (HandleOneObject(pView, point))
				return;
		}
	}
	else
	{
		// no selection, select root
		pView->SelectItem(pDoc->m_pRoot);
	}
}

bool CFormSelectTool::HandleOneObject(CA2FormView* pView, const CPoint& point)
{
	CA2FormDocument* pDoc = pView->GetDocument();
	ATLASSERT(pDoc);
	if (pView->IsInsideEditor())
		return false;
	ATLASSERT(pView->m_selection.GetCount() == 1);

	// one object selected
	if (GetAsyncKeyState(GetSystemMetrics(SM_SWAPBUTTON) ? VK_RBUTTON : VK_LBUTTON) >= 0)
		return FALSE; // Left button already released

	CFormItem* pItem = pView->m_selection.GetHead();
	ATLASSERT(pItem);
	/*
	if (!pItem->m_bFirstClick) {
	if (pItem->OnLButtonDown(pView, 0, point))
	return TRUE;
	}
	*/
	// ��� ����� pItem->OnLButtonDown ��� ���, ��� ����� ������
	// ������� ������ ������
	//if (pItem->GetFlags() & VFITEM_ISLINE)
	//	return FALSE;
	CRect tr(pItem->GetPosition());
	CClientDC dc(pView);
	pView->OnPrepareDC(&dc);
	dc.LPtoDP(&tr); // DocToClient	
	CRectTrackerEx tracker(tr, CRectTracker::resizeOutside, pItem, &dc, &point);
	tracker.m_dwDrawStyle = pItem->GetTrackMask();
	int hit = tracker.HitTest(point);
	if (hit == CRectTracker::hitMiddle && (tracker.m_dwDrawStyle == RTRE_SIZEONLY))
		return FALSE;
	if (hit >= 0) {
		bool bLocked = pDoc->IsLocked();
		tracker.m_sizeMin = pItem->GetMinTrackSize();
		pView->DocToClient(tracker.m_sizeMin);
		if (!bLocked && tracker.Track(pView, point)) {
			// object position has changed
			CRect newRect(tracker.m_rect);
			newRect.NormalizeRect();
			pView->ClientToDoc(newRect);
			//TODO: avoid round errors!!!!
			pItem->DoFitItemRect(newRect);
			//pItem->DoAdjustTrackRect(&newRect, CPoint(0, 0));
			pDoc->m_undo.DoAction(CFormUndo::_change, pItem);
			pItem->MoveTo(newRect);
			return true;
		}
	}
	return false;
}

bool CFormSelectTool::MoveObjects(CA2FormView* pView, const CPoint& point)
{
	CA2FormDocument* pDoc = pView->GetDocument();
	if (pView->IsInsideEditor())
		return false;
	// ��������� ���������� ��������, �������� ������ � Move
	CPoint local(point);
	pView->ClientToDoc(local);
	/*
	CRect tr(pDoc->GetSelectionRect());
	local = tr.TopLeft();
	pView->DocToClient(tr);
	CRectTracker tracker(tr, CRectTracker::resizeOutside);
	if (tracker.HitTest(point) == CRectTracker::hitMiddle) {
		if (tracker.Track(pView, point)) {
			CRect nr(tracker.m_rect);
			nr.NormalizeRect();
			pView->ClientToDoc(nr);
			CPoint delta = (CPoint)(nr.TopLeft() - local);
			//pView->GetDocument()->m_undo.DoAction(CFormUndo::_change, &pView->m_selection);
			POSITION pos = pDoc->m_selectionList.GetHeadPosition();
			while (pos != NULL) {
				CFormItem* pItem = pDoc->m_selectionList.GetNext(pos);
				CRect position(pItem->GetPosition());
				position += delta;
				pItem->MoveTo(position, pView, -1);
			}
			return true;
		}
	}
	*/
	return false;
}
