using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CNCMaps.Shared;

namespace CNCMaps.GUI.Design {
	/// <summary>
	/// Registers DynamicCustomTypeDescriptor providers for the mod-config model types so the
	/// ModConfigEditor's PropertyGrid gets Id-based ordering and StandardValue metadata.
	/// Registration is per type, so it also covers list rows created after the editor opened.
	/// </summary>
	public static class DescriptorInstaller {
		private static bool _installed;

		public static void EnsureInstalled() {
			if (_installed)
				return;
			_installed = true;
			foreach (var type in new[] {
					typeof(ModConfig), typeof(TheaterSettings), typeof(ObjectOverride), typeof(ModOption),
				})
				TypeDescriptor.AddProvider(new DynamicTypeDescriptionProvider(type), type);
		}

		private class DynamicTypeDescriptionProvider : TypeDescriptionProvider {
			// DynamicCustomTypeDescriptor binds to an instance and caches its property
			// descriptors, so keep one per instance without extending its lifetime
			private readonly ConditionalWeakTable<object, DynamicCustomTypeDescriptor> _descriptors =
				new ConditionalWeakTable<object, DynamicCustomTypeDescriptor>();

			public DynamicTypeDescriptionProvider(Type type)
				: base(TypeDescriptor.GetProvider(type)) {
			}

			public override ICustomTypeDescriptor GetTypeDescriptor(Type objectType, object instance) {
				if (instance == null)
					return base.GetTypeDescriptor(objectType, null);
				return _descriptors.GetValue(instance,
					inst => new DynamicCustomTypeDescriptor(base.GetTypeDescriptor(objectType, inst), inst));
			}
		}
	}
}
