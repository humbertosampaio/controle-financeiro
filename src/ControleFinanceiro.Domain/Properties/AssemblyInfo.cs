[assembly: VogenDefaults(
    underlyingType: typeof(Guid),
    conversions: Conversions.Default,
    customizations: Customizations.AddFactoryMethodForGuids,
    staticAbstractsGeneration: StaticAbstractsGeneration.MostCommon)]