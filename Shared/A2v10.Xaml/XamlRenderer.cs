﻿// Copyright © 2015-2017 Alex Kukhtin. All rights reserved.

using System;
using System.IO;
using System.Xaml;

using A2v10.Infrastructure;

namespace A2v10.Xaml
{
	public class XamlRenderer : IRenderer
	{
		private readonly IProfiler _profile;
		private readonly IApplicationHost _host;
		public XamlRenderer(IProfiler profile, IApplicationHost host)
		{
			_profile = profile;
			_host = host;
		}

		public void Render(RenderInfo info)
		{
			if (String.IsNullOrEmpty(info.FileName))
				throw new XamlException("No source for render");
			IProfileRequest request = _profile.CurrentRequest;
			String fileName = String.Empty;
			UIElementBase uiElem = null;
			using (request.Start(ProfileAction.Render, $"load: {info.FileTitle}"))
			{
				// XamlServices.Load sets IUriContext
				if (!String.IsNullOrEmpty(info.FileName))
					uiElem = XamlServices.Load(info.FileName) as UIElementBase;
				else if (!String.IsNullOrEmpty(info.Text))
					uiElem = XamlServices.Parse(info.Text) as UIElementBase;
				else
					throw new XamlException("Xaml. There must be either a 'FileName' or a 'Text' property");
				if (uiElem == null)
					throw new XamlException("Xaml. Root is not 'UIElement'");

				// TODO: may be cached in release configuration
				String stylesPath = _host.MakeFullPath(false, String.Empty, "styles.xaml");
				if (File.Exists(stylesPath)) {
					if (!(XamlServices.Load(stylesPath) is Styles styles))
						throw new XamlException("Xaml. Styles is not 'Styles'");
					if (uiElem is RootContainer root)
					{
						root.Styles = styles;
						root?.OnSetStyles();
					}
				}
			}

			using (request.Start(ProfileAction.Render, $"render: {info.FileTitle}"))
			{
				RenderContext ctx = new RenderContext(uiElem, info)
				{
					RootId = info.RootId,
					Path = info.Path
				};

				if (info.SecondPhase)
				{
					if (!(uiElem is ISupportTwoPhaseRendering twoPhaseRender))
						throw new XamlException("The two-phase rendering is not available");
					twoPhaseRender.RenderSecondPhase(ctx);
				}
				else
				{
					uiElem.RenderElement(ctx);
				}

				Grid.ClearAttached();
				Splitter.ClearAttached();
				FullHeightPanel.ClearAttached();
				Toolbar.ClearAttached();
			}

			if (uiElem is IDisposable disp)
			{
				disp.Dispose();
			}
		}
	}
}
