// solutioncreatedlg.cpp : implementation file
//

#include "stdafx.h"
#include "A2v10.Designer.h"
#include "solutioncreatedlg.h"
#include "afxdialogex.h"


//virtual 
void CA2EditBrowseCtrl::OnBrowse()
{
	DWORD dwFlags = FOS_PATHMUSTEXIST | FOS_PICKFOLDERS;
	CFolderPickerDialog dlg(nullptr, dwFlags, this, sizeof(OPENFILENAME), TRUE);
	if (dlg.DoModal() != IDOK)
		return;
	CString strResult = dlg.GetFolderPath();
	SetWindowText(strResult);
	SetModify(TRUE);
	OnAfterUpdate();
}


// CSolutionCreateDlg dialog

CSolutionCreateDlg::CSolutionCreateDlg(CWnd* pParent /*=nullptr*/)
	: CDialogEx(IDD_SOLUTION_CREATE, pParent)
{
}

CSolutionCreateDlg::~CSolutionCreateDlg()
{
}

void CSolutionCreateDlg::DoDataExchange(CDataExchange* pDX)
{
	CDialogEx::DoDataExchange(pDX);
	DDX_Control(pDX, IDC_FOLDER, m_folder);
	DDX_Control(pDX, IDC_NAME, m_name);
}


BEGIN_MESSAGE_MAP(CSolutionCreateDlg, CDialogEx)
	ON_BN_CLICKED(IDC_BROWSE, &CSolutionCreateDlg::OnBnClickedBrowse)
	ON_BN_CLICKED(IDOK, &CSolutionCreateDlg::OnOk)
END_MESSAGE_MAP()


// virtual 
BOOL CSolutionCreateDlg::OnInitDialog() {
	__super::OnInitDialog();
	m_folder.SetWindowText(m_strFolder);
	m_name.SetWindowText(m_strName);
	return TRUE;
};


void CSolutionCreateDlg::OnBnClickedBrowse()
{
	DWORD dwFlags = FOS_PATHMUSTEXIST | FOS_PICKFOLDERS;
	CFolderPickerDialog dlg(nullptr, dwFlags, this, sizeof(OPENFILENAME), TRUE);
	if (dlg.DoModal() != IDOK)
		return;
	CString strResult = dlg.GetFolderPath();
	m_folder.SetWindowText(strResult);
	m_folder.SetFocus();
	auto len = m_folder.GetWindowTextLength();
	m_folder.SetSel(len, len+1);
}


void CSolutionCreateDlg::OnOk()
{
	m_folder.GetWindowText(m_strFolder);
	m_name.GetWindowText(m_strName);

	if (m_strFolder.IsEmpty() || m_strName.IsEmpty()) {
		AfxMessageBox(L"error!");
		return;
	}
	__super::OnOK();
}
