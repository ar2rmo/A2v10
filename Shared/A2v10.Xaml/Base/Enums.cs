﻿
// Copyright © 2015-2017 Alex Kukhtin. All rights reserved.

namespace A2v10.Xaml
{
	public enum TextAlign
	{
		Default = 0,
		Left = Default,
		Right,
		Center
	}

	public enum TextColor
	{
		Default = 0,
		Gray,
		Danger
	}

	public enum FloatMode
	{
		None,
		Left,
		Right
	}

	public enum VerticalAlign
	{
		Default,
		Top,
		Middle,
		Bottom
	}

	public enum GridLinesVisibility
	{
		None,
		Horizontal,
		Vertical,
		Both
	}

	public enum Orientation
	{
		Vertical,
		Horizontal
	}

	public enum DataType
	{
		String,
		Date,
		DateTime,
		Time,
		Number,
		Currency,
		Boolean,
		Object,
		Period
	}

	public enum ControlSize
	{
		Default = 0,
		Large = 1,
		Medium = Default,
		Small = 2,
		Mini = 3
	};

	public enum TextSize
	{
		Normal,
		Small,
		Big
	}

	public enum WrapMode
	{
		Default,
		Wrap,
		NoWrap,
		PreWrap
	}

	public enum AutoSelectMode
	{
		None,
		FirstItem,
		ItemId
	}

	public enum RowMarkerStyle
	{
		None,
		Row,
		Marker,
		Both
	}

	public enum MarkStyle
	{
		Default,
		Success,
		Green,
		Warning,
		Orange,
		Info,
		Cyan,
		Danger,
		Red,
		Error
	}

	public enum DropDownDirection
	{
		DownRight,
		DownLeft,
		UpLeft,
		UpRight,
		Default = DownRight
	}

	public enum BackgroundStyle
	{
		None,
		LightGray,
		WhiteSmoke,
		White
	}

	public enum ShadowStyle
	{
		None,
		Shadow1,
		Shadow2,
		Shadow3,
		Shadow4,
		Shadow5
	}

	public enum RenderMode
	{
		Show,
		Hide,
		ReadOnly,
		Debug
	}

	public enum Icon
	{
		NoIcon = 0,
		Access,
		Add,
		AddressBook,
		AddressCard,
		Alert,
		Approve,
		ArrowDown,
		ArrowDownRed,
		ArrowLeft,
		ArrowRight,
		ArrowUp,
		ArrowUpGreen,
		ArrowExport,
		ArrowOpen,
		ArrowSort,
		Attach,
		Ban,
		BanRed,
		Bank,
		BankAccount,
		BankUah,
		BrandExcel,
		Calendar,
		Call,
		ChartArea,
		ChartBar,
		ChartColumn,
		ChartPie,
		ChartPivot,
		ChartStackedArea,
		ChartStackedBar,
		ChartStackedLine,
		Check,
		ChevronDown,
		ChevronLeft,
		ChevronDoubleLeft,
		ChevronLeftEnd,
		ChevronRight,
		ChevronDoubleRight,
		ChevronRightEnd,
		ChevronUp,
		Clear,
		Close,
		Cloud,
		CloudOutline,
		Comment,
		CommentAdd,
		CommentDiscussion,
		CommentLines,
		CommentNext,
		CommentOutline,
		CommentPrevious,
		CommentUrgent,
		CurrencyUah,
		CurrencyUsd,
		CurrencyEuro,
		CurrencyOther,
		Copy,
		Cut,
		Delete,
		DeleteBox,
		Dashboard,
		DashboardOutline,
		Database,
		Devices,
		Disapprove,
		Dot,
		DotBlue,
		DotGreen,
		DotRed,
		Download,
		Edit,
		Ellipsis,
		EllipsisVertical,
		Error,
		ErrorOutline,
		Exit,
		Export,
		ExportExcel,
		External,
		Eye,
		EyeDisabled,
		EyeDisabledRed,
		Failure,
		FailureRed,
		FailureOutline,
		Flag,
		FlagBlue,
		FlagGreen,
		FlagRed,
		FlagYellow,
		File,
		FileContent,
		FileError,
		FileImage,
		FileImport,
		FileLink,
		FilePreview,
		FileSignature,
		FileUser,
		Filter,
		FilterOutline,
		Folder,
		FolderQuery,
		Gear,
		GearOutline,
		Grid,
		Help,
		HelpBlue,
		HelpOutline,
		History,
		Info,
		InfoBlue,
		InfoOutline,
		Image,
		Import,
		Items,
		Link,
		List,
		ListBullet,
		Lock,
		LockOutline,
		Log,
		Menu,
		Message,
		MessageOutline,
		Minus,
		MinusBox,
		MinusCircle,
		Package,
		PackageOutline,
		PaneLeft,
		PaneLeftBlue,
		PaneRight,
		PaneRightBlue,
		Paste,
		Pencil,
		PencilOutline,
		Pin,
		PinOutline,
		Pinned,
		PinnedOutline,
		Plus,
		PlusBox,
		PlusCircle,
		Policy,
		Power,
		Print,
		Process,
		Query,
		Queue,
		Refresh,
		Reload,
		Rename,
		Requery,
		Save,
		SaveClose,
		SaveCloseOutline,
		SaveOutline,
		Search,
		Security,
		Server,
		Share,
		Square,
		Star,
		StarOutline,
		StarYellow,
		Step,
		Steps,
		Storyboard,
		Success,
		SuccessGreen,
		SuccessOutline,
		Switch,
		Table,
		Tag,
		TagBlue,
		TagGreen,
		TagOutline,
		TagRed,
		TagYellow,
		Trash,
		TriangleLeft,
		TriangleRight,
		Unlock,
		UnlockOutline,
		Unpin,
		UnpinOutline,
		Upload,
		User,
		UserMinus,
		UserPlus,
		Users,
		Waiting,
		WaitingOutline,
		Warning,
		WarningOutline,
		WarningYellow,
		Workflow1,
		Wrench
	}
}
