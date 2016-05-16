// ------------------------------------------------------------------------------
//   <copyright from='2010' to='2015' company='THEHACKERWITHIN.COM'>
//     Copyright (c) TheHackerWithin.COM. All Rights Reserved.
// 
//     Please look in the accompanying license.htm file for the license that
//     applies to this source code. (a copy can also be found at:
//     http://www.thehackerwithin.com/license.htm)
//   </copyright>
// -------------------------------------------------------------------------------

namespace DirectEve
{
	using System.Collections.Generic;
	using PySharp;
	using System;

	public class DirectInvType : DirectObject
	{
		private double? _basePrice;
		private double? _capacity;
		private int? _categoryId;
		//private string _categoryName;
		private double? _chanceOfDuplicating;
		private int? _dataId;
		private string _description;
		private int? _graphicId;
		private int? _groupId;
		private string _groupName;
		private int? _iconId;
		private int? _marketGroupId;
		private double? _mass;
		private int? _portionSize;
		private bool? _published;
		//private PyObject _pyInvCategory;
		private PyObject _pyInvGroup;
		private PyObject _pyInvType;



		private int? _raceId;
		private double? _radius;
		private int? _soundId;
		private string _typeName;
		private double? _volume;

		private double? _shield;
		private double? _armor;
		private double? _structure;

		private double? _shieldResistanceEM;
		private double? _shieldResistanceKinetic;
		private double? _shieldResistanceExplosion;
		private double? _shieldResistanceThermal;

		private double? _armorResistanceEM;
		private double? _armorResistanceKinetic;
		private double? _armorResistanceExplosion;
		private double? _armorResistanceThermal;

		private double? _signatureRadius;
		private double? _averagePrice;

		internal DirectInvType(DirectEve directEve)
			: base(directEve)
		{
		}

		internal DirectInvType(DirectEve directEve, int typeId)
			: base(directEve)
		{
			TypeId = typeId;

		}

		internal PyObject PyInvType
		{
			get { return _pyInvType ?? (_pyInvType = PySharp.Import("evetypes").Attribute("storages").Attribute("TypeStorage").Attribute("_storage").DictionaryItem(TypeId)); }
		}

		internal PyObject PyInvGroup
		{
			get { return _pyInvGroup ?? (_pyInvGroup = PySharp.Import("evetypes").Attribute("storages").Attribute("GroupStorage").Attribute("_storage").DictionaryItem(GroupId)); }
		}

		public int TypeId { get; internal set; }

		//     quote = sm.GetService('marketQuote')
		// averagePrice = quote.GetAveragePrice(typeID)
		public double GetAverAgePrice
		{
			get
			{
				if (_averagePrice == null) {
					_averagePrice = (double)DirectEve.GetLocalSvc("marketQuote").Call("GetAveragePrice", TypeId);
				}

				return _averagePrice == null ? 0 : (double)_averagePrice;
			}
		}

		public int GroupId
		{
			get
			{
				if (!_groupId.HasValue)
					_groupId = (int)PyInvType.Attribute("groupID");

				return _groupId.Value;
			}
		}

		public string GroupName
		{
			get
			{
				if (string.IsNullOrEmpty(_groupName))
					_groupName = (string)PyInvGroup;
				return _groupName;
			}
		}

		//public string CategoryName
		//{
		//    get
		//    {
		//        if (string.IsNullOrEmpty(_categoryName))
		//            _categoryName = (string)_pyInvCategory;
		//        return _categoryName;
		//    }
		//}

		public string TypeName
		{
			get
			{
				if (string.IsNullOrEmpty(_typeName))
					_typeName = (string)PySharp.Import("evetypes").Attribute("localizationUtils").Call("GetLocalizedTypeName", (int)PyInvType.Attribute("typeNameID"), "en-us");

				return _typeName;
			}
		}

		public string Description
		{
			get
			{
				if (string.IsNullOrEmpty(_description))
					_description = (string)PyInvType.Attribute("description");

				return _description;
			}
		}

		public int GraphicId
		{
			get
			{
				if (!_graphicId.HasValue)
					_graphicId = (int)PyInvType.Attribute("graphicID");

				return _graphicId.Value;
			}
		}

		public double Radius
		{
			get
			{
				if (!_radius.HasValue)
					_radius = (double)PyInvType.Attribute("radius");

				return _radius.Value;
			}
		}

		public double Mass
		{
			get
			{
				if (!_mass.HasValue)
					_mass = (double)PyInvType.Attribute("mass");

				return _mass.Value;
			}
		}

		public double Volume
		{
			get
			{
				if (!_volume.HasValue)
					_volume = (double)PyInvType.Attribute("volume");

				return _volume.Value;
			}
		}

		public double Capacity
		{
			get
			{
				if (!_capacity.HasValue)
					_capacity = (double)PyInvType.Attribute("capacity");

				return _capacity.Value;
			}
		}

		public int PortionSize
		{
			get
			{
				if (!_portionSize.HasValue)
					_portionSize = (int)PyInvType.Attribute("portionSize");

				return _portionSize.Value;
			}
		}

		public int RaceId
		{
			get
			{
				if (!_raceId.HasValue)
					_raceId = (int)PyInvType.Attribute("raceID");

				return _raceId.Value;
			}
		}

		public double BasePrice
		{
			get
			{
				if (!_basePrice.HasValue)
					_basePrice = (double)PyInvType.Attribute("basePrice");

				return _basePrice.Value;
			}
		}

		public bool Published
		{
			get
			{
				if (!_published.HasValue)
					_published = (bool)PyInvType.Attribute("published");

				return _published.Value;
			}
		}

		public int MarketGroupId
		{
			get
			{
				if (!_marketGroupId.HasValue)
					_marketGroupId = (int)PyInvType.Attribute("marketGroupID");

				return _marketGroupId.Value;
			}
		}


		public double ChanceOfDuplicating
		{
			get
			{
				if (!_chanceOfDuplicating.HasValue)
					_chanceOfDuplicating = (double)PyInvType.Attribute("chanceOfDuplicating");

				return _chanceOfDuplicating.Value;
			}
		}

		public int SoundId
		{
			get
			{
				if (!_soundId.HasValue)
					_soundId = (int)PyInvType.Attribute("soundID");

				return _soundId.Value;
			}
		}

		public int CategoryId
		{
			get
			{
				if (!_categoryId.HasValue)
					_categoryId = (int)PyInvGroup.Attribute("categoryID");

				return _categoryId.Value;
			}
		}

		public int IconId
		{
			get
			{
				if (!_iconId.HasValue)
					_iconId = (int)PyInvType.Attribute("iconID");

				return _iconId.Value;
			}
		}

		public int DataId
		{
			get
			{
				if (!_dataId.HasValue)
					_dataId = (int)PyInvType.Attribute("dataID");

				return _dataId.Value;
			}
		}

		public double? Shield
		{
			get
			{
				if (!_shield.HasValue)
				{
					_shield = TryGet<float>("shieldCapacity");
				}
				return _shield;
			}
		}

		public double? Armor
		{
			get
			{
				if (!_armor.HasValue)
				{
					_armor = TryGet<float>("armorHP");
				}
				return _armor;
			}
		}

		public double? Structure
		{
			get
			{
				if (!_structure.HasValue)
				{
					_structure = TryGet<float>("hp");
				}
				return _structure;
			}
		}

		public double? ShieldResistanceEM
		{
			get
			{
				if (!_shieldResistanceEM.HasValue)
				{
					_shieldResistanceEM = TryGet<float>("shieldEmDamageResonance");
				}
				return _shieldResistanceEM;
			}
		}

		public double? ShieldResistanceKinetic
		{
			get
			{
				if (!_shieldResistanceKinetic.HasValue)
				{
					_shieldResistanceKinetic = TryGet<float>("shieldKineticDamageResonance");
				}
				return _shieldResistanceKinetic;
			}
		}

		public double? ShieldResistanceExplosion
		{
			get
			{
				if (!_shieldResistanceExplosion.HasValue)
				{
					_shieldResistanceExplosion = TryGet<float>("shieldExplosiveDamageResonance");
				}
				return _shieldResistanceExplosion;
			}
		}

		public double? ShieldResistanceThermal
		{
			get
			{
				if (!_shieldResistanceThermal.HasValue)
				{
					_shieldResistanceThermal = TryGet<float>("shieldThermalDamageResonance");
				}
				return _shieldResistanceThermal;
			}
		}

		public double? ArmorResistanceEM
		{
			get
			{
				if (!_armorResistanceEM.HasValue)
				{
					_armorResistanceEM = TryGet<float>("armorEmDamageResonance");
				}
				return _armorResistanceEM;
			}
		}

		public double? ArmorResistanceKinetic
		{
			get
			{
				if (!_armorResistanceKinetic.HasValue)
				{
					_armorResistanceKinetic = TryGet<float>("armorKineticDamageResonance");
				}
				return _armorResistanceKinetic;
			}
		}

		public double? ArmorResistanceExplosion
		{
			get
			{
				if (!_armorResistanceExplosion.HasValue)
				{
					_armorResistanceExplosion = TryGet<float>("armorExplosiveDamageResonance");
				}
				return _armorResistanceExplosion;
			}
		}

		public double? ArmorResistanceThermal
		{
			get
			{
				if (!_armorResistanceThermal.HasValue)
				{
					_armorResistanceThermal = TryGet<float>("armorThermalDamageResonance");
				}
				return _armorResistanceThermal;
			}
		}

		public double? SignatureRadius
		{
			get
			{
				if (!_signatureRadius.HasValue)
				{
					_signatureRadius = new int?((int)TryGet<float>("signatureRadius")) ?? 0;
				}
				return _signatureRadius.Value;
			}
		}


		Dictionary<string, object> _attrdictionary;
		public Dictionary<string, object> GetAttributesInvType()
		{
			if (_attrdictionary == null)
			{
				_attrdictionary = new Dictionary<string, object>();
				foreach (PyObject pyitem in dmgAttributes())
				{
					try
					{
						int itemkeyattr = (int)pyitem.Attribute("attributeID");
						string itemattr = (string)DirectEve.PySharp.Import("__builtin__").Attribute("cfg").Attribute("dgmattribs").DictionaryItem(itemkeyattr).Attribute("attributeName");

						object type = null;
						PyObject pyitemvalue = pyitem.Attribute("value");
						switch (pyitemvalue.GetPyType())
						{
							case PyType.IntType:
								type = pyitemvalue.ToInt();
								break;
							case PyType.LongType:
								type = pyitemvalue.ToLong();
								break;
							case PyType.StringType:
							case PyType.UnicodeType:
								type = pyitemvalue.ToUnicodeString();
								break;
							case PyType.BoolType:
								type = pyitemvalue.ToBool();
								break;
							case PyType.FloatType:
								type = pyitemvalue.ToFloat();
								break;
							default:
								DirectEve.Log("DirectItemAttributes Item Unknow type [" + itemattr + "]");
								break;
						}
						_attrdictionary[itemattr] = type;
					}
					catch (Exception ex)
					{
						DirectEve.Log("DirectInvTypes.GetAttributes exception:" + ex);
					}
				}
			}
			return _attrdictionary;
		}

		public T TryGet<T>(string keyname)
		{
			object obj = null;
			if (GetAttributesInvType().ContainsKey(keyname))
			{
				object item = GetAttributesInvType()[keyname];
				if (item != null)
				{
					if (typeof(T) == typeof(bool))
					{
						obj = (int)item;
						return (T)obj;
					}
					if (typeof(T) == typeof(string))
					{
						obj = (string)item;
						return (T)obj;
					}
					if (typeof(T) == typeof(int))
					{
						obj = (int)item;
						return (T)obj;
					}
					if (typeof(T) == typeof(long))
					{
						obj = (long)item;
						return (T)obj;
					}
					if (typeof(T) == typeof(float))
					{
						obj = (float)item;
						return (T)obj;
					}
					if (typeof(T) == typeof(double))
					{
						obj = Convert.ToDouble(item);
						return (T)obj;
					}
					if (typeof(T) == typeof(DateTime))
					{
						obj = Convert.ToDateTime(item);
						return (T)obj;
					}
				}
			}
			return default(T);
		}

		List<PyObject> _dmgAttributes;
		List<PyObject> dmgAttributes()
		{
			if (_dmgAttributes == null)
			{
				
				PyObject _dmgAttribute = DirectEve.PySharp.Import("__builtin__").Attribute("cfg").Attribute("dgmtypeattribs").Call("get", TypeId);
				if (!_dmgAttribute.IsValid)
					return null;
				_dmgAttributes = _dmgAttribute.ToList();
			}
			return _dmgAttributes;
		}

		internal static Dictionary<int, DirectInvType> GetInvtypes(DirectEve directEve)
		{
			var result = new Dictionary<int, DirectInvType>();

			var pyDict = directEve.PySharp.Import("evetypes").Attribute("storages").Attribute("TypeStorage").Attribute("_storage").ToDictionary<int>();
			foreach (var pair in pyDict)
				result[pair.Key] = new DirectInvType(directEve, pair.Key);

			return result;
		}
		
		internal static DirectInvType GetInvType(DirectEve directEve, int typeId) {
			
			var pyDictItem = directEve.PySharp.Import("evetypes").Attribute("storages").Attribute("TypeStorage").Attribute("_storage").DictionaryItem(typeId);
			
			if(pyDictItem != null && pyDictItem.IsValid)
				return new DirectInvType(directEve, typeId);
			
			return null;;
		}


	}
}