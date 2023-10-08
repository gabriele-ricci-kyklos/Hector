using System;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Hector.Data.Dynamic
{
    internal static class DynamicMapperHelper
    {
        internal static MethodInfo GetValueMethod { get; } = typeof(IDataRecord).GetMethod("get_Item", new Type[] { typeof(int) });
        internal static MethodInfo IsDBNullMethod { get; } = typeof(IDataRecord).GetMethod(nameof(IDataRecord.IsDBNull), new Type[] { typeof(int) });
        internal static MethodInfo ChangeTypeMethod { get; } = typeof(Convert).GetMethod(nameof(Convert.ChangeType), new[] { typeof(object), typeof(Type) });
        internal static MethodInfo GetTypeMethod { get; } = typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle), new[] { typeof(RuntimeTypeHandle) });
        internal static MethodInfo EnumParse { get; } = typeof(Enum).GetMethod(nameof(Enum.Parse), new[] { typeof(Type), typeof(string), typeof(bool) });

        internal delegate object Load(IDataRecord dataRecord);

        internal static GenericDataReaderToEntityDynamicMapper CreateMapperImpl
        (
            Type type,
            Type destinationType,
            IDataRecord dataRecord,
            bool ignoreCase,
            Func<string, bool>? isStringDataTypeFx = null,
            Func<PropertyInfo, string>? property2FieldNameMapping = null
        )
        {
            isStringDataTypeFx ??= (x => false);
            property2FieldNameMapping ??= (x => x.Name);

            DynamicMethod method =
                new DynamicMethod
                (
                    $"{nameof(DataReaderToEntityDynamicMapperFactory)}_{type.FullName}Create",
                    destinationType,
                    new Type[] { typeof(IDataRecord) },
                    destinationType,
                    true
                );

            ILGenerator generator = method.GetILGenerator();

            LocalBuilder result = generator.DeclareLocal(destinationType);
            LocalBuilder destTypeLocVar = generator.DeclareLocal(typeof(Type));
            LocalBuilder drValueLocVar = generator.DeclareLocal(typeof(object));

            //  var xx = destinationType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic);

            generator.Emit(OpCodes.Newobj, destinationType.GetConstructor(Type.EmptyTypes));
            generator.Emit(OpCodes.Stloc, result);

            var propsByFieldName =
                destinationType
                .GetProperties()
                .ToDictionary
                (
                    x => property2FieldNameMapping(x),
                    ignoreCase ? StringComparer.InvariantCultureIgnoreCase : StringComparer.InvariantCulture
                );

            var props =
                destinationType
                .GetProperties()
                .ToDictionary
                (
                    x => x.Name,
                    ignoreCase ? StringComparer.InvariantCultureIgnoreCase : StringComparer.InvariantCulture
                );

            int mappedProperties = 0;
            for (int i = 0; i < dataRecord.FieldCount; ++i)
            {
                PropertyInfo? propertyInfo = null;
                props.TryGetValue(dataRecord.GetName(i), out propertyInfo);

                if (propertyInfo is null)
                {
                    propsByFieldName.TryGetValue(dataRecord.GetName(i), out propertyInfo);
                }

                if (propertyInfo is null)
                {
                    continue;
                }

                ++mappedProperties;

                string datatypeName = dataRecord.GetDataTypeName(i);
                Label endIfLabel = generator.DefineLabel();

                if (propertyInfo.GetSetMethod() != null)
                {
                    generator.Emit(OpCodes.Ldarg_0);
                    generator.Emit(OpCodes.Ldc_I4, i);
                    generator.Emit(OpCodes.Callvirt, DynamicMapperHelper.IsDBNullMethod);
                    generator.Emit(OpCodes.Brtrue, endIfLabel);

                    generator.Emit(OpCodes.Ldloc, result);
                    generator.Emit(OpCodes.Ldarg_0);
                    generator.Emit(OpCodes.Ldc_I4, i);
                    generator.Emit(OpCodes.Callvirt, DynamicMapperHelper.GetValueMethod);

                    if (propertyInfo.PropertyType.IsGenericType
                        && propertyInfo.PropertyType.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
                    {
                        var nullableType = propertyInfo.PropertyType.GetGenericTypeDefinition().GetGenericArguments()[0];
                        var underlyingType = Nullable.GetUnderlyingType(propertyInfo.PropertyType);

                        Type emitType = underlyingType;

                        bool emitEnumParse = false;
                        if (underlyingType.IsEnum)
                        {
                            if (isStringDataTypeFx(datatypeName))
                            {
                                emitEnumParse = true;
                            }
                            else
                            {
                                emitType = Enum.GetUnderlyingType(underlyingType);
                            }
                        }

                        if (emitEnumParse)
                        {
                            ILGeneratorDataInfo genInfo =
                                new ILGeneratorDataInfo
                                (
                                    generator,
                                    destTypeLocVar,
                                    drValueLocVar,
                                    emitType
                                );

                            EmitEnumParse(genInfo);
                        }

                        generator.Emit(OpCodes.Stloc, drValueLocVar);
                        generator.Emit(OpCodes.Ldtoken, emitType);
                        generator.Emit(OpCodes.Call, DynamicMapperHelper.GetTypeMethod);
                        generator.Emit(OpCodes.Stloc, destTypeLocVar);
                        generator.Emit(OpCodes.Ldloc, drValueLocVar);
                        generator.Emit(OpCodes.Ldloc, destTypeLocVar);
                        generator.Emit(OpCodes.Call, DynamicMapperHelper.ChangeTypeMethod);
                        generator.Emit(OpCodes.Unbox_Any, underlyingType);
                        generator.Emit(OpCodes.Newobj, propertyInfo.PropertyType.GetConstructor(new Type[] { underlyingType }));
                    }
                    else
                    {
                        Type emitType = propertyInfo.PropertyType;
                        bool emitEnumParse = false;
                        if (propertyInfo.PropertyType.IsEnum)
                        {
                            if (isStringDataTypeFx(datatypeName))
                            {
                                emitEnumParse = true;
                            }
                            else
                            {
                                emitType = Enum.GetUnderlyingType(propertyInfo.PropertyType);
                            }
                        }

                        if (emitEnumParse)
                        {
                            ILGeneratorDataInfo genInfo =
                                new ILGeneratorDataInfo
                                (
                                    generator,
                                    destTypeLocVar,
                                    drValueLocVar,
                                    emitType
                                );
                            EmitEnumParse(genInfo);
                        }

                        generator.Emit(OpCodes.Stloc, drValueLocVar);
                        generator.Emit(OpCodes.Ldtoken, emitType);
                        generator.Emit(OpCodes.Call, GetTypeMethod);
                        generator.Emit(OpCodes.Stloc, destTypeLocVar);
                        generator.Emit(OpCodes.Ldloc, drValueLocVar);
                        generator.Emit(OpCodes.Ldloc, destTypeLocVar);
                        generator.Emit(OpCodes.Call, ChangeTypeMethod);
                        generator.Emit(OpCodes.Unbox_Any, propertyInfo.PropertyType);
                    }
                    generator.Emit(OpCodes.Callvirt, propertyInfo.GetSetMethod());

                    generator.MarkLabel(endIfLabel);
                }
            }

            generator.Emit(OpCodes.Ldloc, result);
            generator.Emit(OpCodes.Ret);

            return new GenericDataReaderToEntityDynamicMapper((Load)method.CreateDelegate(typeof(Load)), mappedProperties);
        }

        internal static void EmitEnumParse(ILGeneratorDataInfo ilInfo)
        {
            ilInfo.ILGenerator.Emit(OpCodes.Stloc, ilInfo.DrValueLocVar);
            ilInfo.ILGenerator.Emit(OpCodes.Ldtoken, ilInfo.EmitType);
            ilInfo.ILGenerator.Emit(OpCodes.Call, GetTypeMethod);
            ilInfo.ILGenerator.Emit(OpCodes.Stloc, ilInfo.DestTypeLocVar);
            ilInfo.ILGenerator.Emit(OpCodes.Ldloc, ilInfo.DestTypeLocVar);
            ilInfo.ILGenerator.Emit(OpCodes.Ldloc, ilInfo.DrValueLocVar);
            ilInfo.ILGenerator.Emit(OpCodes.Ldc_I4_1, ilInfo.DrValueLocVar);
            ilInfo.ILGenerator.Emit(OpCodes.Call, EnumParse);
        }

        internal record ILGeneratorDataInfo
        (
            ILGenerator ILGenerator,
            LocalBuilder DestTypeLocVar,
            LocalBuilder DrValueLocVar,
            Type EmitType
        );
    }
}
