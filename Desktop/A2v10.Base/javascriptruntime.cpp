// Copyright � 2008-2017 Alex Kukhtin. All rights reserved.

#include "stdafx.h"

#include "../include/javascriptpropertyid.h"
#include "../include/javascriptvalue.h"
#include "../include/javascriptruntime.h"
#include "../include/javascriptnative.h"
#include "../include/javascriptexceptions.h"

#include "../include/appdefs.h"
#include "../include/filetools.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#endif

JsRuntimeHandle s_runtime = JS_INVALID_RUNTIME_HANDLE;
volatile bool s_bInDebugMode = false;
volatile bool s_bClosingProgress = false;

JsSourceContext s_currentContext = JS_SOURCE_CONTEXT_NONE;

// static 
JsRuntimeHandle JavaScriptRuntime::CurrentRuntime()
{
	if (s_runtime != JS_INVALID_RUNTIME_HANDLE)
		return s_runtime;
	JsContextRef context = JS_INVALID_REFERENCE;
	JavaScriptNative::ThrowIfError(JsGetCurrentContext(&context));
	JavaScriptNative::ThrowIfError(JsGetRuntime(context, &s_runtime));
	return s_runtime;
}

JsValueRef CHAKRA_CALLBACK RequireCallback(_In_ JsValueRef callee, _In_ bool isConstructCall, _In_ JsValueRef *arguments, _In_ unsigned short argumentCount, _In_opt_ void *callbackState)
{
	CString msg;
	try
	{
		if (argumentCount < 3)
			throw JavaScriptUsageException(JsErrorCode::JsErrorInvalidArgument, L"__require");
		CString fileName = JavaScriptValue(arguments[1]).ConvertToString().ToString();
		CString pathName = JavaScriptValue(arguments[2]).ConvertToString().ToString();
		WCHAR fullPath[_MAX_PATH + 1];
		::PathCombine(fullPath, pathName, fileName);
		::PathAddExtension(fullPath, L".js");
		if (!::PathFileExists(fullPath)) {
			msg.Format(L"File '%s' not found", fullPath);
			throw JavaScriptUsageException(JsErrorCode::JsErrorInvalidArgument, msg);
		}
		CString code;
		if (!CFileTools::LoadFile(fullPath, code)) {
			msg.Format(L"Error reading from file '%s'", fullPath);
			throw JavaScriptUsageException(JsErrorCode::JsErrorScriptException, msg);
		}
		return JavaScriptRuntime::RunModule(code, fullPath);
	}
	catch (JavaScriptException& jsEx) 
	{
		jsEx.SetException();
	}
	catch (...)
	{
		JavaScriptRuntime::SetUnknownException();
	}
	return JS_INVALID_REFERENCE;
}

JsValueRef CHAKRA_CALLBACK AlertCallback(_In_ JsValueRef callee, _In_ bool isConstructCall, _In_ JsValueRef *arguments, _In_ unsigned short argumentCount, _In_opt_ void *callbackState)
{
	try {
		CString strMessage(EMPTYSTR);
		if (argumentCount > 1)
			strMessage = JavaScriptValue(arguments[1]).ConvertToString().ToString();
		AfxMessageBox(strMessage);
	}
	catch (JavaScriptException& jsEx)
	{
		jsEx.SetException();
	}
	catch (...)
	{
		JavaScriptRuntime::SetUnknownException();
	}
	return JS_INVALID_REFERENCE;
}

JsValueRef CHAKRA_CALLBACK LogCallback(_In_ JsValueRef callee, _In_ bool isConstructCall, _In_ JsValueRef *arguments, _In_ unsigned short argumentCount, _In_opt_ void *callbackState) 
{
	try 
	{
		CString strMessage(EMPTYSTR);
		if (argumentCount > 1)
			strMessage = JavaScriptValue(arguments[1]).ConvertToString().ToString();
		WPARAM wParam = reinterpret_cast<WPARAM>(callbackState);
		CWnd* pWnd = AfxGetMainWnd();
		if (pWnd) {
			pWnd->SendMessage(WMI_CONSOLE, wParam, (LPARAM)(LPCWSTR)strMessage);
		}
	}
	catch (JavaScriptException& jsEx) 
	{
		jsEx.SetException();
	}
	catch (...) 
	{
		JavaScriptRuntime::SetUnknownException();
	}
	return JS_INVALID_REFERENCE;
}

// static
void JavaScriptRuntime::CreateGlobalObject()
{
	// in CURRENT (global) context
	auto glob = JavaScriptValue::GlobalObject();
	
	auto alert = JavaScriptValue::CreateFunction(AlertCallback, nullptr);
	glob.SetProperty(L"alert", alert);
	
	auto console = JavaScriptValue::CreateObject();
	glob.SetProperty(L"console", console);
	
	auto log = JavaScriptValue::CreateFunction(LogCallback, (void*) WMI_CONSOLE_LOG);
	console.SetProperty(L"log", log);

	auto require = JavaScriptValue::CreateFunction(RequireCallback, nullptr);
	glob.SetProperty(L"__require", require);

}

// static 
CString JavaScriptRuntime::Evaluate(const wchar_t* szScript)
{
	JavaScriptValue result;
	JavaScriptNative::ThrowIfError(JsRunScript(szScript, JS_SOURCE_CONTEXT_NONE, L"", result));
	return result.ConvertToString().ToString();
}

bool JavaScriptRuntime::RunScript(LPCWSTR szCode, LPCWSTR szPathName)
{
	JavaScriptValue result = JS_INVALID_REFERENCE;
	s_currentContext += 1;
	int context = s_currentContext;
	JavaScriptNative::ThrowIfError(JsRunScript(szCode, context, szPathName, result));
	return s_bClosingProgress;
}

JavaScriptValue JavaScriptRuntime::RunModule(LPCWSTR szCode, LPCWSTR szPathName)
{
	JavaScriptValue result = JS_INVALID_REFERENCE;
	s_currentContext += 1;
	int context = s_currentContext;
	LPCWSTR szPrologue = L"(function() { let m = {exports: {}}; (function(module, exports) { ";
	LPCWSTR szEpilogue = L" })(m, m.exports); return m.exports;})();";
	CString codeToRun(szPrologue);
	codeToRun += szCode;
	codeToRun += szEpilogue;
	JavaScriptNative::ThrowIfError(JsRunScript(codeToRun, context, szPathName, result));
	return result;
}

// static 
void JavaScriptRuntime::SetException(JavaScriptValue exception)
{
	JavaScriptNative::ThrowIfError(JsSetException(exception));
}

// static
void JavaScriptRuntime::SetUnknownException()
{
	auto err = JavaScriptValue::CreateError(JavaScriptValue::FromString(L"Unknown error"));
	JavaScriptRuntime::SetException(err);
}


// static 
JsContextRef JavaScriptRuntime::CreateContext()
{
	JsContextRef newContext = JS_INVALID_REFERENCE;
	JavaScriptNative::ThrowIfError(JsCreateContext(CurrentRuntime(), &newContext));
	return newContext;
}

// static 
JavaScriptValue JavaScriptRuntime::CreateDesignerElement(const wchar_t* szJson)
{
	auto createElem = JavaScriptValue::GlobalObject().GetPropertyChain(L"designer.form.__createElement");
	return createElem.CallFunctionArg(JavaScriptValue::FromString(szJson));
}

static int processEvents()
{
	CWinThread* pThis = AfxGetApp();
	ASSERT_VALID(pThis);

	_AFX_THREAD_STATE* pState = AfxGetThreadState();

	// for tracking the idle time state
	BOOL bIdle = TRUE;
	LONG lIdleCount = 0;

	// acquire and dispatch messages until a WM_QUIT message is received.
	for (;;)
	{
		// phase1: check to see if we can do idle work
		while (bIdle &&
			!::PeekMessage(&(pState->m_msgCur), NULL, NULL, NULL, PM_NOREMOVE))
		{
			// call OnIdle while in bIdle state
			if (!pThis->OnIdle(lIdleCount++))
				bIdle = FALSE; // assume "no idle" state
		}

		// phase2: pump messages while available
		do
		{
			// pump message, but quit on WM_QUIT
			if (!pThis->PumpMessage()) {
				// simply complete DiagDebugEventCallback and send quit message again
				JavaScriptRuntime::SetDebugMode(false);
				s_bClosingProgress = true;
				PostQuitMessage(0);
				return 0;
			}
			// reset "no idle" state after pumping "normal" message
			//if (IsIdleMessage(&m_msgCur))
			if (pThis->IsIdleMessage(&(pState->m_msgCur)))
			{
				bIdle = TRUE;
				lIdleCount = 0;
			}
			if (!JavaScriptRuntime::InDebugMode()) {
				JavaScriptRuntime::ExitDebugMode();
				return 0;
			}

		} while (::PeekMessage(&(pState->m_msgCur), NULL, NULL, NULL, PM_NOREMOVE));
	}
}

static void _sendDebugInfo(JsValueRef eventData)
{
	if (s_bClosingProgress)
		return;
	JavaScriptValue eventInfo(eventData);

	int lineNo = eventInfo.GetProperty(L"line").ToInt();
	int scriptId = eventInfo.GetProperty(L"scriptId").ToInt();

	CString fileName = JavaScriptRuntime::GetFileNameFromScriptId(scriptId);

	DEBUG_BREAK_INFO breakInfo;
	breakInfo.szFileName = (LPCWSTR) fileName;
	breakInfo.scriptId = scriptId;
	breakInfo.lineNo = lineNo;
	AfxGetMainWnd()->SendMessage(WMI_DEBUG_BREAK, WMI_DEBUG_BREAK_WPARAM, (LPARAM)&breakInfo);
}

CString JavaScriptRuntime::GetFileNameFromScriptId(int scriptId)
{
	JavaScriptValue arr;
	JavaScriptNative::ThrowIfError(JsDiagGetScripts(arr));
	int len = arr.GetProperty(L"length").ToInt();
	auto fileNamePropId = JavaScriptPropertyId::FromString(L"fileName");
	auto scriptIdPropId = JavaScriptPropertyId::FromString(L"scriptId");
	for (int i = 0; i < len; i++) {
		JavaScriptValue item = arr.GetProperty(i);
		int itemId = item.GetProperty(scriptIdPropId).ToInt();
		if (itemId == scriptId) {
			JavaScriptValue fileNameVal = item.GetProperty(fileNamePropId);
			if (fileNameVal.ValueType() == JsString) {
				return fileNameVal.ToString();
			}
		}
	}
	return L"";
}

void CHAKRA_CALLBACK DiagDebugEventCallback(_In_ JsDiagDebugEvent debugEvent, _In_ JsValueRef eventData, _In_opt_ void* callbackState)
{
	if (s_bClosingProgress)
		return;
	if ((debugEvent == JsDiagDebugEvent::JsDiagDebugEventDebuggerStatement) || 
		(debugEvent == JsDiagDebugEvent::JsDiagDebugEventStepComplete) || 
		(debugEvent == JsDiagDebugEvent::JsDiagDebugEventBreakpoint)) 
	{
		JavaScriptRuntime::SetDebugMode(true);
		JavaScriptRuntime::EnterDebugMode();
		_sendDebugInfo(eventData);
		//auto str = JavaScriptValue::GlobalObject().GetPropertyChain(L"JSON.stringify");
		//auto data = str.CallFunction(JavaScriptValue::Undefined(), eventData).ToString();
		processEvents();
	}
}

void JavaScriptRuntime::StartDebugging() 
{
	JavaScriptNative::ThrowIfError(JsDiagStartDebugging(CurrentRuntime(), DiagDebugEventCallback, nullptr));
}

void JavaScriptRuntime::StopDebugging()
{
	void* pState = nullptr;
	JavaScriptNative::ThrowIfError(JsDiagStopDebugging(CurrentRuntime(), &pState));
}

// static 
bool JavaScriptRuntime::InDebugMode()
{
	return s_bInDebugMode;
}

void JavaScriptRuntime::SetDebugMode(bool bSet) 
{
	if (s_bInDebugMode == bSet)
		return;
	s_bInDebugMode = bSet;
	if (bSet)
		AfxGetMainWnd()->PostMessage(WMI_DEBUG_MODE, WMI_DEBUG_MODE_WPARAM, (LPARAM)TRUE);
}

// static 
void JavaScriptRuntime::EndRunScript()
{
	AfxGetMainWnd()->PostMessage(WMI_DEBUG_MODE, WMI_DEBUG_MODE_WPARAM, (LPARAM)FALSE);
}

void JavaScriptRuntime::ExitDebugMode()
{
}

// static 
void JavaScriptRuntime::EnterDebugMode()
{
}

// static 
void JavaScriptRuntime::SetDebugStepType(DebugStepType step)
{
	JsDiagStepType dt = JsDiagStepTypeContinue;
	switch (step) {
	case StepIn: dt = JsDiagStepTypeStepIn; break;
	case StepOut: dt = JsDiagStepTypeStepOut; break;
	case StepOver: dt = JsDiagStepTypeStepOver; break;
	case Continue: dt = JsDiagStepTypeContinue; break;
	default:
		ATLASSERT(FALSE);
	}
	JavaScriptNative::ThrowIfError(JsDiagSetStepType(dt));
}

JavaScriptContext::JavaScriptContext()
{
	JavaScriptNative::ThrowIfError(JsGetCurrentContext(&m_prevContext));
	JsContextRef newContext = JavaScriptRuntime::CreateContext();

	auto globSrc = JavaScriptValue::GlobalObject();
	JavaScriptNative::ThrowIfError(JsSetCurrentContext(newContext));
	auto globTrg = JavaScriptValue::GlobalObject();

	auto alertPropId = JavaScriptPropertyId::FromString(L"alert");
	auto consolePropId = JavaScriptPropertyId::FromString(L"console");
	auto requirePropId = JavaScriptPropertyId::FromString(L"__require");
	auto alertVal = globTrg.GetProperty(alertPropId);
	if (alertVal.ValueType() != JsUndefined)
		return; // already set
	alertVal = globSrc.GetProperty(alertPropId);
	globTrg.SetProperty(alertPropId, alertVal);
	auto consoleVal = globSrc.GetProperty(consolePropId);
	globTrg.SetProperty(consolePropId, consoleVal);
	auto requireVal = globSrc.GetProperty(requirePropId);
	globTrg.SetProperty(requirePropId, requireVal);
	//TODO: parse app objects in this context (app, require, etc)
}

JavaScriptContext::~JavaScriptContext()
{
	JavaScriptNative::ThrowIfError(JsSetCurrentContext(m_prevContext));
}

