﻿namespace ChakraHost.Hosting
{
	using System;

	/// <summary>
	///     A property identifier.
	/// </summary>
	/// <remarks>
	///     Property identifiers are used to refer to properties of JavaScript objects instead of using
	///     strings.
	/// </remarks>
#pragma warning disable IDE0049 // Use framework type
#pragma warning disable IDE0018 // Inline variable declaration
	public struct JavaScriptPropertyId : IEquatable<JavaScriptPropertyId>
	{
		/// <summary>
		/// The id.
		/// </summary>
		private readonly IntPtr id;

		/// <summary>
		///     Initializes a new instance of the <see cref="JavaScriptPropertyId"/> struct. 
		/// </summary>
		/// <param name="id">The ID.</param>
		internal JavaScriptPropertyId(IntPtr id)
		{
			this.id = id;
		}

		/// <summary>
		///     Gets an invalid ID.
		/// </summary>
		public static JavaScriptPropertyId Invalid
		{
			get { return new JavaScriptPropertyId(IntPtr.Zero); }
		}

		/// <summary>
		///     Gets the name associated with the property ID.
		/// </summary>
		/// <remarks>
		///     <para>
		///     Requires an active script context.
		///     </para>
		/// </remarks>
		public string Name
		{
			get
			{
				string name;
				NativeMethods.ThrowIfError(NativeMethods.JsGetPropertyNameFromId(this, out name));
				return name;
			}
		}

		/// <summary>
		///     Gets the property ID associated with the name. 
		/// </summary>
		/// <remarks>
		///     <para>
		///     Property IDs are specific to a context and cannot be used across contexts.
		///     </para>
		///     <para>
		///     Requires an active script context.
		///     </para>
		/// </remarks>
		/// <param name="name">
		///     The name of the property ID to get or create. The name may consist of only digits.
		/// </param>
		/// <returns>The property ID in this runtime for the given name.</returns>
		public static JavaScriptPropertyId FromString(string name)
		{
			JavaScriptPropertyId id;
			NativeMethods.ThrowIfError(NativeMethods.JsGetPropertyIdFromName(name, out id));
			return id;
		}

		/// <summary>
		///     The equality operator for property IDs.
		/// </summary>
		/// <param name="left">The first property ID to compare.</param>
		/// <param name="right">The second property ID to compare.</param>
		/// <returns>Whether the two property IDs are the same.</returns>
		public static bool operator ==(JavaScriptPropertyId left, JavaScriptPropertyId right)
		{
			return left.Equals(right);
		}

		/// <summary>
		///     The inequality operator for property IDs.
		/// </summary>
		/// <param name="left">The first property ID to compare.</param>
		/// <param name="right">The second property ID to compare.</param>
		/// <returns>Whether the two property IDs are not the same.</returns>
		public static bool operator !=(JavaScriptPropertyId left, JavaScriptPropertyId right)
		{
			return !left.Equals(right);
		}

		/// <summary>
		///     Checks for equality between property IDs.
		/// </summary>
		/// <param name="other">The other property ID to compare.</param>
		/// <returns>Whether the two property IDs are the same.</returns>
		public bool Equals(JavaScriptPropertyId other)
		{
			return id == other.id;
		}

		/// <summary>
		///     Checks for equality between property IDs.
		/// </summary>
		/// <param name="obj">The other property ID to compare.</param>
		/// <returns>Whether the two property IDs are the same.</returns>
		public override bool Equals(object obj)
		{
#pragma warning disable IDE0041 // Use 'is null' check
			if (ReferenceEquals(null, obj))
			{
				return false;
			}
#pragma warning restore IDE0041 // Use 'is null' check

			return obj is JavaScriptPropertyId && Equals((JavaScriptPropertyId)obj);
		}

		/// <summary>
		///     The hash code.
		/// </summary>
		/// <returns>The hash code of the property ID.</returns>
		public override int GetHashCode()
		{
			return id.ToInt32();
		}

		/// <summary>
		///     Converts the property ID to a string.
		/// </summary>
		/// <returns>The name of the property ID.</returns>
		public override string ToString()
		{
			return Name;
		}
	}
#pragma warning restore IDE0018 // Inline variable declaration
#pragma warning restore IDE0049 // Use framework type
}