using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace RCS.LogViewer.Model;

internal class AppSettings : INotifyPropertyChanged, IEditableObject
{
	public event PropertyChangedEventHandler? PropertyChanged;

	void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

	#region Properties

	string? _subscriptionId;
	[Category("Azure")]
	[DisplayName("Subscription Id")]
	[Description("A Guid-like string taken from the Azure portal, Subscriptions, Account blade.")]
	public string? SubscriptionId
	{
		get => _subscriptionId;
		set
		{
			string? newval = string.IsNullOrEmpty(value) ? null : value;
			if (_subscriptionId != newval)
			{
				_subscriptionId = newval;
				OnPropertyChanged(nameof(SubscriptionId));
				OnPropertyChanged(nameof(HasAzureSettings));
			}
		}
	}

	string? _tenantId;
	[Category("Azure")]
	[DisplayName("Tenant Id")]
	[Description("A Guid-like string taken from the Azure portal, Active Directory blade.")]
	public string? TenantId
	{
		get => _tenantId;
		set
		{
			string? newval = string.IsNullOrEmpty(value) ? null : value;
			if (_tenantId != newval)
			{
				_tenantId = newval;
				OnPropertyChanged(nameof(TenantId));
				OnPropertyChanged(nameof(HasAzureSettings));
			}
		}
	}

	string? _applicationId;
	[Category("Azure")]
	[DisplayName("Application Id")]
	[Description("A Guid-like string taken from the Azure portal, Active Directory, App Registrations, Application Client Id. An app must be registered with AD, then in the Subscriptions, IAM blade it is given the 'Owner' role assignment for the subscription.")]
	public string? ApplicationId
	{
		get => _applicationId;
		set
		{
			string? newval = string.IsNullOrEmpty(value) ? null : value;
			if (_applicationId != newval)
			{
				_applicationId = newval;
				OnPropertyChanged(nameof(ApplicationId));
				OnPropertyChanged(nameof(HasAzureSettings));
			}
		}
	}

	string? _clientSecret;
	[Category("Azure")]
	[DisplayName("Client Secret")]
	[Description("A 'Client secret' created in the Active Directory, App registrations, Certificates and secrets blade for the app identified by Application Id.")]
	public string? ClientSecret
	{
		get => _clientSecret;
		set
		{
			string? newval = string.IsNullOrEmpty(value) ? null : value;
			if (_clientSecret != newval)
			{
				_clientSecret = newval;
				OnPropertyChanged(nameof(ClientSecret));
				OnPropertyChanged(nameof(HasAzureSettings));
			}
		}
	}

	[Browsable(false)]
	public bool HasAzureSettings => Guid.TryParse(_subscriptionId, out var _) && Guid.TryParse(_tenantId, out var _) && Guid.TryParse(_applicationId, out var _) && _clientSecret?.Length >= 40;

	#endregion

	#region IEditableObject

	public IEnumerable<PropertyInfo> WalkProps() => GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanWrite);

	Dictionary<string, object?>? saveProps;
	string? oldSubId;
	string? oldTenId;
	string? oldAppId;
	string? oldSecret;

	public void BeginEdit()
	{
		Debug.Assert(saveProps == null);
		oldSubId = _subscriptionId;
		oldTenId = _tenantId;
		oldAppId = _applicationId;
		oldSecret = _clientSecret;
		saveProps = [];
		foreach (var prop in WalkProps())
		{
			saveProps[prop.Name] = prop.GetValue(this);
		}
	}

	public void CancelEdit()
	{
		Debug.Assert(saveProps != null);
		foreach (var prop in WalkProps())
		{
			prop.SetValue(this, saveProps[prop.Name]);
		}
		saveProps = null;
	}

	[Obsolete("Use CheckedEndEdit instead to know what things might have changed.")]
	public void EndEdit()
	{
		Debug.Assert(saveProps != null);
		saveProps = null;
	}

	public void CheckedEndEdit(out bool credentialsChanged)
	{
		credentialsChanged =
			string.CompareOrdinal(_subscriptionId, oldSubId) != 0 ||
			string.CompareOrdinal(_tenantId, oldTenId) != 0 ||
			string.CompareOrdinal(_applicationId, oldAppId) != 0 ||
			string.CompareOrdinal(_clientSecret, oldSecret) != 0;
#pragma warning disable CS0618 // This is a wrapper over the fake obsolete method                                                    
		EndEdit();
#pragma warning restore CS0618
	}

	#endregion
}
