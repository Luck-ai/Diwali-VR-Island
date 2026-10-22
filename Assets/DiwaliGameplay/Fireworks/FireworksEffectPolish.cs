using UnityEngine;
namespace DiwaliGameplay {
public static class FireworksEffectPolish {
 public static void Configure(FireworksGame game){
  var fountain=game.fountainEffect;fountain.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
  var main=fountain.main;main.playOnAwake=false;main.loop=true;main.duration=2;main.simulationSpace=ParticleSystemSimulationSpace.World;main.startLifetime=new ParticleSystem.MinMaxCurve(.8f,1.15f);main.startSpeed=new ParticleSystem.MinMaxCurve(3.8f,5.2f);main.startSize=new ParticleSystem.MinMaxCurve(.025f,.055f);main.startColor=new ParticleSystem.MinMaxGradient(new Color(1,.66f,.15f),new Color(1,.94f,.6f));main.gravityModifier=.65f;main.maxParticles=240;
  var emission=fountain.emission;emission.rateOverTime=105;emission.SetBursts(new ParticleSystem.Burst[0]);var shape=fountain.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=17;shape.radius=.065f;
  var trails=fountain.trails;trails.enabled=true;trails.ratio=.55f;trails.lifetime=.14f;trails.minVertexDistance=.04f;FestivalParticleStyle.Apply(fountain);
  var spinner=game.spinnerEffect;spinner.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);spinner.transform.localPosition=Vector3.zero;spinner.transform.localRotation=Quaternion.identity;
  main=spinner.main;main.playOnAwake=false;main.loop=true;main.duration=1;main.maxParticles=1;emission=spinner.emission;emission.rateOverTime=0;emission.SetBursts(new ParticleSystem.Burst[0]);
  float radius=Mathf.Clamp(GameObjectUtility.BoundsOf(game.spinner.transform).extents.magnitude*.65f,.15f,1.1f);
  for(int i=0;i<4;i++){
   float angle=i*Mathf.PI*.5f;var radial=new Vector3(0,Mathf.Cos(angle),Mathf.Sin(angle));var tangent=Vector3.Cross(Vector3.right,radial);
   var go=new GameObject("Spinner golden jet "+(i+1));go.transform.SetParent(spinner.transform,false);go.transform.localPosition=radial*radius;go.transform.localRotation=Quaternion.LookRotation(tangent,Vector3.right);
   var jet=go.AddComponent<ParticleSystem>();jet.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);main=jet.main;main.playOnAwake=false;main.loop=true;main.duration=1;main.simulationSpace=ParticleSystemSimulationSpace.World;main.startLifetime=new ParticleSystem.MinMaxCurve(.45f,.8f);main.startSpeed=new ParticleSystem.MinMaxCurve(2.2f,3.4f);main.startSize=new ParticleSystem.MinMaxCurve(.035f,.065f);main.startColor=new Color(1,.78f,.23f);main.gravityModifier=.18f;main.maxParticles=80;emission=jet.emission;emission.rateOverTime=40;shape=jet.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=10;shape.radius=.018f;trails=jet.trails;trails.enabled=true;trails.ratio=.8f;trails.lifetime=.24f;trails.minVertexDistance=.035f;FestivalParticleStyle.Apply(jet);
  }
  spinner.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
 }
}
}
