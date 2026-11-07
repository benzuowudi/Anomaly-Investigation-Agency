using System.Collections.Generic;
public class AOTGenericReferences : UnityEngine.MonoBehaviour
{

	// {{ AOT assemblies
	public static readonly IReadOnlyList<string> PatchedAOTAssemblyList = new List<string>
	{
		"DOTween.dll",
		"LubanLib.dll",
		"System.Core.dll",
		"UnityEngine.AssetBundleModule.dll",
		"UnityEngine.CoreModule.dll",
		"mscorlib.dll",
	};
	// }}

	// {{ constraint implement type
	// }} 

	// {{ AOT generic types
	// DG.Tweening.Core.DOGetter<UnityEngine.Vector2>
	// DG.Tweening.Core.DOGetter<float>
	// DG.Tweening.Core.DOSetter<UnityEngine.Vector2>
	// DG.Tweening.Core.DOSetter<float>
	// System.Action<object>
	// System.Collections.Generic.Dictionary.Enumerator<cfg.GameType.UINameType,object>
	// System.Collections.Generic.Dictionary.Enumerator<int,object>
	// System.Collections.Generic.Dictionary.Enumerator<object,object>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<object,object>
	// System.Collections.Generic.Dictionary.KeyCollection<object,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<int,object>
	// System.Collections.Generic.Dictionary.ValueCollection<int,object>
	// System.Collections.Generic.Dictionary<cfg.GameType.TileType,object>
	// System.Collections.Generic.Dictionary<cfg.GameType.UINameType,object>
	// System.Collections.Generic.Dictionary<int,byte>
	// System.Collections.Generic.Dictionary<int,object>
	// System.Collections.Generic.Dictionary<object,object>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.IEnumerable<object>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.IEnumerator<object>
	// System.Collections.Generic.KeyValuePair<cfg.GameType.UINameType,object>
	// System.Collections.Generic.KeyValuePair<object,object>
	// System.Collections.Generic.List.Enumerator<int>
	// System.Collections.Generic.List.Enumerator<object>
	// System.Collections.Generic.List<byte>
	// System.Collections.Generic.List<cfg.GameType.UINameType>
	// System.Collections.Generic.List<cfg.GameType.WeatherType>
	// System.Collections.Generic.List<float>
	// System.Collections.Generic.List<int>
	// System.Collections.Generic.List<object>
	// System.Collections.Generic.Stack<object>
	// System.Func<System.Collections.Generic.KeyValuePair<object,object>,byte>
	// System.Func<object,object>
	// System.Func<object>
	// System.Nullable<byte>
	// System.Nullable<double>
	// System.Nullable<float>
	// System.Nullable<int>
	// System.Nullable<long>
	// System.Nullable<short>
	// }}

	public void RefMethods()
	{
		// string Bright.Common.StringUtil.CollectionToString<cfg.GameType.WeatherType>(System.Collections.Generic.IEnumerable<cfg.GameType.WeatherType>)
		// string Bright.Common.StringUtil.CollectionToString<float>(System.Collections.Generic.IEnumerable<float>)
		// string Bright.Common.StringUtil.CollectionToString<int,object>(System.Collections.Generic.IDictionary<int,object>)
		// string Bright.Common.StringUtil.CollectionToString<int>(System.Collections.Generic.IEnumerable<int>)
		// string Bright.Common.StringUtil.CollectionToString<object>(System.Collections.Generic.IEnumerable<object>)
		// object DG.Tweening.TweenSettingsExtensions.OnComplete<object>(object,DG.Tweening.TweenCallback)
		// object System.Activator.CreateInstance<object>()
		// byte[] System.Array.Empty<byte>()
		// System.Collections.Generic.KeyValuePair<object,object> System.Linq.Enumerable.ElementAt<System.Collections.Generic.KeyValuePair<object,object>>(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,object>>,int)
		// System.Collections.Generic.KeyValuePair<object,object> System.Linq.Enumerable.First<System.Collections.Generic.KeyValuePair<object,object>>(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,object>>)
		// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,object>> System.Linq.Enumerable.Where<System.Collections.Generic.KeyValuePair<object,object>>(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,object>>,System.Func<System.Collections.Generic.KeyValuePair<object,object>,bool>)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter,Framework.Script.UI.Anim_Change.<Anim>d__6>(System.Runtime.CompilerServices.TaskAwaiter&,Framework.Script.UI.Anim_Change.<Anim>d__6&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter,Framework.Script.UI.Anim_Change.<Anim>d__7>(System.Runtime.CompilerServices.TaskAwaiter&,Framework.Script.UI.Anim_Change.<Anim>d__7&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter,Framework.Script.UI.Anim_Deformation.<Anim>d__3>(System.Runtime.CompilerServices.TaskAwaiter&,Framework.Script.UI.Anim_Deformation.<Anim>d__3&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter,Framework.Script.UI.Anim_Deformation.<Anim>d__4>(System.Runtime.CompilerServices.TaskAwaiter&,Framework.Script.UI.Anim_Deformation.<Anim>d__4&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter,Framework.Script.UI.Anim_Pan.<Anim>d__3>(System.Runtime.CompilerServices.TaskAwaiter&,Framework.Script.UI.Anim_Pan.<Anim>d__3&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter,Framework.Script.UI.Anim_Pan.<Anim>d__4>(System.Runtime.CompilerServices.TaskAwaiter&,Framework.Script.UI.Anim_Pan.<Anim>d__4&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter,Framework.Script.UI.Anim_Reveal.<Anim>d__3>(System.Runtime.CompilerServices.TaskAwaiter&,Framework.Script.UI.Anim_Reveal.<Anim>d__3&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter,Framework.Script.UI.Anim_Reveal.<Anim>d__4>(System.Runtime.CompilerServices.TaskAwaiter&,Framework.Script.UI.Anim_Reveal.<Anim>d__4&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter,GameMain.Script.UI.BattleOrderRing.<<HideRingUI>b__22_0>d>(System.Runtime.CompilerServices.TaskAwaiter&,GameMain.Script.UI.BattleOrderRing.<<HideRingUI>b__22_0>d&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter,GameMain.Script.UI.BattleOrderRing.<<ShowRingUI>b__21_0>d>(System.Runtime.CompilerServices.TaskAwaiter&,GameMain.Script.UI.BattleOrderRing.<<ShowRingUI>b__21_0>d&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.Start<Framework.Script.UI.Anim_Change.<Anim>d__6>(Framework.Script.UI.Anim_Change.<Anim>d__6&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.Start<Framework.Script.UI.Anim_Change.<Anim>d__7>(Framework.Script.UI.Anim_Change.<Anim>d__7&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.Start<Framework.Script.UI.Anim_Deformation.<Anim>d__3>(Framework.Script.UI.Anim_Deformation.<Anim>d__3&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.Start<Framework.Script.UI.Anim_Deformation.<Anim>d__4>(Framework.Script.UI.Anim_Deformation.<Anim>d__4&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.Start<Framework.Script.UI.Anim_Pan.<Anim>d__3>(Framework.Script.UI.Anim_Pan.<Anim>d__3&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.Start<Framework.Script.UI.Anim_Pan.<Anim>d__4>(Framework.Script.UI.Anim_Pan.<Anim>d__4&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.Start<Framework.Script.UI.Anim_Reveal.<Anim>d__3>(Framework.Script.UI.Anim_Reveal.<Anim>d__3&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.Start<Framework.Script.UI.Anim_Reveal.<Anim>d__4>(Framework.Script.UI.Anim_Reveal.<Anim>d__4&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.Start<Framework.Script.UI.UIAnim.<Anim>d__4>(Framework.Script.UI.UIAnim.<Anim>d__4&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.Start<Framework.Script.UI.UIAnim.<Anim>d__5>(Framework.Script.UI.UIAnim.<Anim>d__5&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.Start<GameMain.Script.UI.BattleOrderRing.<<HideRingUI>b__22_0>d>(GameMain.Script.UI.BattleOrderRing.<<HideRingUI>b__22_0>d&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.Start<GameMain.Script.UI.BattleOrderRing.<<ShowRingUI>b__21_0>d>(GameMain.Script.UI.BattleOrderRing.<<ShowRingUI>b__21_0>d&)
		// System.Void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter,Framework.Script.UI.UIWindow.<EnterAnim>d__10>(System.Runtime.CompilerServices.TaskAwaiter&,Framework.Script.UI.UIWindow.<EnterAnim>d__10&)
		// System.Void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter,GameMain.Script.UI.CommandUI.<OnClickShowBtn>d__43>(System.Runtime.CompilerServices.TaskAwaiter&,GameMain.Script.UI.CommandUI.<OnClickShowBtn>d__43&)
		// System.Void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter,GameMain.Script.UI.CommandUI.<OnOnClickHideBtn>d__44>(System.Runtime.CompilerServices.TaskAwaiter&,GameMain.Script.UI.CommandUI.<OnOnClickHideBtn>d__44&)
		// System.Void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.Start<Framework.Script.UI.UIWindow.<EnterAnim>d__10>(Framework.Script.UI.UIWindow.<EnterAnim>d__10&)
		// System.Void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.Start<GameMain.Script.UI.CommandUI.<OnClickShowBtn>d__43>(GameMain.Script.UI.CommandUI.<OnClickShowBtn>d__43&)
		// System.Void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.Start<GameMain.Script.UI.CommandUI.<OnOnClickHideBtn>d__44>(GameMain.Script.UI.CommandUI.<OnOnClickHideBtn>d__44&)
		// System.Threading.Tasks.Task<object> System.Threading.Tasks.TaskFactory.StartNew<object>(System.Func<object>)
		// object UnityEngine.AssetBundle.LoadAsset<object>(string)
		// object UnityEngine.Component.GetComponent<object>()
		// object UnityEngine.GameObject.AddComponent<object>()
		// object UnityEngine.GameObject.GetComponent<object>()
		// object UnityEngine.Object.Instantiate<object>(object)
		// object UnityEngine.Object.Instantiate<object>(object,UnityEngine.Transform)
		// object UnityEngine.Object.Instantiate<object>(object,UnityEngine.Vector3,UnityEngine.Quaternion,UnityEngine.Transform)
	}
}