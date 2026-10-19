[assembly: VogenDefaults(
    underlyingType: typeof(Guid),
    conversions: Conversions.Default,
    customizations: Customizations.AddFactoryMethodForGuids,
    // Como o MostCommon, mas com o cast para o primitivo implícito: o id vira Guid sem expor o .Value.
    staticAbstractsGeneration: StaticAbstractsGeneration.ValueObjectsDeriveFromTheInterface
        | StaticAbstractsGeneration.EqualsOperators
        | StaticAbstractsGeneration.ExplicitCastFromPrimitive
        | StaticAbstractsGeneration.ImplicitCastToPrimitive
        | StaticAbstractsGeneration.FactoryMethods,
    toPrimitiveCasting: CastOperator.Implicit)]