
#include "stdafx.h"

// SHARED_HANDLERS can be defined in an ATL project implementing preview, thumbnail
// and search filter handlers and allows sharing of document code with that project.
#ifndef SHARED_HANDLERS
#include "A2v10.Designer.h"
#endif

#include "formitem.h"
#include "a2formdoc.h"

#include "mainfrm.h"
#include "recttracker.h"

#include "elemform.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#endif


IMPLEMENT_DYNCREATE(CA2FormDocument, CDocument)

CA2FormDocument::CA2FormDocument()
	: m_pRoot(nullptr)
{
}

// virtual 
CA2FormDocument::~CA2FormDocument()
{
	Clear();
	ATLASSERT(m_pRoot == nullptr);
}

void CA2FormDocument::Clear() 
{
	if (m_pRoot)
		delete m_pRoot;
	m_pRoot = nullptr;
}

BEGIN_MESSAGE_MAP(CA2FormDocument, CDocument)
END_MESSAGE_MAP()

// virtual 
BOOL CA2FormDocument::OnNewDocument()
{
	if (!__super::OnNewDocument())
		return FALSE;
	CreateRootElement();
	return TRUE;
}

void CA2FormDocument::CreateRootElement()
{
	ATLASSERT(m_pRoot == nullptr);
	m_pRoot = new CFormElement();
}

bool CA2FormDocument::IsLocked() const
{
	return false;
}

// virtual 
void CA2FormDocument::OnCloseDocument()
{
	Clear();
	__super::OnCloseDocument();
}

// virtual 
BOOL CA2FormDocument::OnOpenDocument(LPCTSTR lpszPathName)
{
	return __super::OnOpenDocument(lpszPathName);
}

// virtual 
BOOL CA2FormDocument::OnSaveDocument(LPCTSTR lpszPathName)
{
	try 
	{
		tinyxml2::XMLDocument doc;
		auto root = doc.NewElement(L"Form");
		root->SetAttribute(L"xmlns", L"clr-namespace:A2v10.Xaml;assembly=A2v10.Xaml");
		root->SetAttribute(L"xmlns:x", L"http://schemas.microsoft.com/winfx/2006/xaml");
		root->SetAttribute(L"Width", 123);
		root->SetAttribute(L"Height", 123);
		doc.InsertEndChild(root);
		auto tb = doc.NewElement(L"Form.Toolbar");
		root->InsertEndChild(tb);
		tb->InsertEndChild(doc.NewElement(L"Grid"));
		tinyxml2::XMLPrinter printer;
		doc.Print(&printer);
		AfxMessageBox(printer.CStr());

		tinyxml2::XMLDocument pdoc;
		pdoc.Parse(printer.CStr());
		tinyxml2::XMLPrinter printer2;
		pdoc.Print(&printer2);
		AfxMessageBox(printer2.CStr());
		//m_pRoot->SaveToXaml(doc);
		//auto error = doc.SaveFile(path);
		//CXmlFile file(lpszPathName, L"xaml");
		//m_pRoot->SaveToXaml(file);
		//file.Write();
	}
	catch (int /*CXmlError& err*/) 
	{
		//err.ReportError();
		return FALSE;
	}
	return TRUE;
}

//virtual 
BOOL CA2FormDocument::CanCloseFrame(CFrameWnd* pFrame)
{
	if (!__super::CanCloseFrame(pFrame))
		return FALSE;
	return TRUE;
}

// virtual 
void CA2FormDocument::Serialize(CArchive& ar)
{
	ATLASSERT(FALSE);
}

// virtual 
void CA2FormDocument::SetModifiedFlag(BOOL bModified /*= TRUE*/)
{
	__super::SetModifiedFlag(bModified);
}

void CA2FormDocument::DrawContent(const RENDER_INFO& ri)
{
	ATLASSERT(m_pRoot != nullptr);
	ri.pDC->SetBkMode(TRANSPARENT);
	HGDIOBJ pOldFont = ri.pDC->SelectObject(CTheme::GetUIFont(CTheme::FontUiDefault));

	m_pRoot->Draw(ri);
	DrawSelection(ri);

	ri.pDC->SelectObject(pOldFont);
}

void CA2FormDocument::DrawSelection(const RENDER_INFO& ri)
{
	CFormItem* pItem = m_pRoot;
	bool bNotFirst = false;
	CRect xr(pItem->GetPosition());
	ri.pDC->LPtoDP(xr);
	CRectTrackerEx tr(xr, CRectTracker::resizeOutside);
	tr.m_dwDrawStyle = pItem->GetTrackMask();
	bool bOutline = bNotFirst; // || m_bOrderMode || m_bInsideEditor || GetDocument()->IsControlsLocked()
	tr.DrawItem(ri.pDC, bOutline);
}