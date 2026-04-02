using Friflo.Engine.ECS;
using ReadyM.Api.ECS.Components;
using UnrealEngine.Engine;

[assembly:GlobalGenericInstanceType(typeof(MappingComponent<>), nameof(MappingComponent<>.GameObject), typeof(AActor))]
