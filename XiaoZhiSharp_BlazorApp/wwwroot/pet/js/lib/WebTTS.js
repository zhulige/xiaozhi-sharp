/**
 * @file WebTTS.js
 * @description 基于 speechSynthesis API 的语音合成封装
 * 脚本运行会创建名为 WebTTS 的全局构造器，可实例化后使用；
 * 语音配置支持读取与保存，创建对象时进行读取，网页离开时进行保存；
 * 读取与保存途径可传入回调函数自定义，默认保存在当前网页的 localStorage，其他保存途径如 cooking、扩展后台、服务器等，
 * 语音参数支持热键控制，可控制项包括 语速、音量、音调的大小，语音的角色；
 * 语音参数的控制热键需在创建对象后调用方法进行绑定或移除；
 * date: 2021-04-19
 * @author Leon <Leon_101@foxmail.com>
 */

(function () {
	//检查浏览器环境是否支持所需的API
	if (window.speechSynthesis === undefined) {
		console.error("当前浏览器不支持 speechSynthesis ，初始化已取消！");
		return;
	}
	if (window.SpeechSynthesisUtterance === undefined) {
		console.error("当前浏览器不支持 SpeechSynthesisUtterance ，初始化已取消！");
		return;
	}

	//创建全局的 WebTTS 构造器
	if (window.WebTTS === undefined) {
		window.WebTTS = WebTTS;
	} else {
		console.error("WebTTS 已存在，初始化取消！");
		return;
	}


	function WebTTS(readConfig = readConfigByDefault, writeConfig = writeConfigByDefault) {
		//构造器调用检查
		if (!new.target) {
			console.error("构造器 WebTTS 需要使用 new 调用！");
			return;
		}

		let webTTS = this;  //定义实例对象的别名，在嵌套函数中使用
		let utter = new SpeechSynthesisUtterance();
		
		this.utter = utter;
		this._voiceList = [];  //保存获取到的语音列表
		this._voiceName = "";  //保存使用的语音名称
		
		//定义访问器属性 voiceList 与 voiceName，当其变化时，通知voice尝试更新
		Object.defineProperty(this, "voiceList", {
			get() {
				return this._voiceList;
			},
			set(newValue) {
				//console.log("voiceList.length" + newValue.length);
				newValue = webTTS.filterVoiceList(newValue);  //过滤语音列表
				this._voiceList = newValue;
				updateVoice(this);  //尝试更新语音
			}
		});
		Object.defineProperty(this, "voiceName", {
			get() {
				return this._voiceName;
			},
			set(newValue) {
				this._voiceName = newValue;
				updateVoice(this);
			}
		});

		//封装 utter 的属性，语速、音量、音调，取值区间统一映射为 0 - 100，消除原生的浮点误差
		Object.defineProperty(this, "rate", {
			get() {
				return (
					utter.rate * ((utter.voice && !utter.voice.localService) ? 50 : 10)
				).toFixed(2) - 0;
			},
			set(newValue) {
				utter.rate = (
					newValue / ((utter.voice && !utter.voice.localService) ? 50 : 10)
				).toFixed(2) - 0;
			}
		});

		Object.defineProperty(this, "pitch", {
			get() {
				return (utter.pitch * 50).toFixed(2) - 0;
			},
			set(newValue) {
				utter.pitch = (newValue / 50).toFixed(2) - 0;
			}
		});

		Object.defineProperty(this, "volume", {
			get() {
				return (utter.volume * 100).toFixed(2) - 0;
			},
			set(newValue) {
				utter.volume = (newValue / 100).toFixed(2) - 0;
			}
		});

		//获取语音列表--回调执行
		speechSynthesis.addEventListener("voiceschanged", voiceschanged);
		voiceschanged();
		function voiceschanged(e) {
			webTTS.voiceList = speechSynthesis.getVoices();
		}
		

		//读取配置数据（可异步进行）
		readConfig(this);

		//离开网页时保存配置
		window.addEventListener("unload", function (e) {
			writeConfig(webTTS);
		}, false);
	}


	/**
	 * @method
	 * @description 根据预先的配置合成并朗读传入的文本
	 * @param {string} text 需朗读的文本
	 * @param {boolean} isWait 是否等待进行中的朗读
	 */
	WebTTS.prototype.speak = function (text, isWait = false) {
		this.utter.text = text;
		if (!isWait) speechSynthesis.cancel();  //取消之前的朗读
		speechSynthesis.speak(this.utter);
	}

	/**
	 * @method
	 * @description 停止进行中的朗读
	 */
	WebTTS.prototype.stop = function () {
		speechSynthesis.cancel();
	}

	/**
	 * @method
	 * @description 添加控制语音参数的热键
	 */
	WebTTS.prototype.addHotkey = function () {
		//debugger;
		//过滤重复调用
		if (this.utterController) return;
		
		//创建控制器对象
		this.utterController = new UtterController(this, text => {
			this.speak(text);
		});
		document.addEventListener(
			"keydown",
			this.utterController.hotkeyFunc,
			false
		);
	}

	/**
	 * @method
	 * @description 移除控制语音参数的热键
	 */
	WebTTS.prototype.removeHotkey = function () {
		document.removeEventListener(
			"keydown",
			this.utterController.hotkeyFunc,
			false
		);
		this.utterController = null;  //回收内存
	}

	/**
	 * @method
	 * @param {object} oldList 就列表
	 * @returns {object} 过滤后的新列表
	 */
WebTTS.prototype.filterVoiceList = function(oldList) {
	return oldList.filter(item=>true);
}

	/**
	 * @description 默认的读取配置的函数，
	 * @param {object} tts 接收配置参数的对象
	 */
	function readConfigByDefault(tts) {
		let webTTSData = localStorage.getItem("webTTSData");
		if (webTTSData) {
			webTTSData = JSON.parse(webTTSData);
		} else {
			webTTSData = {};
		}
		//tts.lang = webTTSData.lang === undefined ? "zh-CN" : webTTSData.lang;
		tts.rate = webTTSData.rate === undefined ? 50 : webTTSData.rate;
		tts.pitch = webTTSData.pitch === undefined ? 50 : webTTSData.pitch;
		tts.volume = webTTSData.volume === undefined ? 100 : webTTSData.volume;
		tts.voiceName = webTTSData.voiceName === undefined ? "Microsoft Huihui Desktop - Chinese (Simplified)" : webTTSData.voiceName;
		//console.log(tts.voiceName)
	}

	/**
	 * @description 默认的保存配置的函数，保存位置为 localStorage
	 * @param {object} tts 需保存的对象
	 */
	function writeConfigByDefault(tts) {
		let webTTSData = {};
		//webTTSData.lang = tts.lang;
		webTTSData.rate = tts.rate;
		webTTSData.pitch = tts.pitch;
		webTTSData.volume = tts.volume;
		webTTSData.voiceName = tts.voiceName;
		webTTSData = JSON.stringify(webTTSData);
		localStorage.setItem("webTTSData", webTTSData);
	}


	/**
	 * @constructor
	 * @description 语音参数的控制器
	 * @param {object} target 需控制的目标对象
	 * @param {FunctionStringCallback} callback 控制执行后的回调
	 */
	function UtterController(target, callback = text => { }) {
		this.target = target;
		this.callback = callback;
		this.hotkeyFunc = this._hotkeyFunc.bind(this);
		this.index = 0;  //当前选项的索引
		this.options = [  //保存参数选项的数组
			{ key: "rate", name: "语速", minValue: 0, maxValue: 100, step: 5 },
			{ key: "volume", name: "音量", minValue: 0, maxValue: 100, step: 5 },
			{ key: "pitch", name: "音调", minValue: 0, maxValue: 100, step: 5 },
			{
				key: "voiceList", name: "语音", subKey: "name", minValue: 0,
				get maxValue() { return target.voiceList.length - 1 },
				set maxValue(newValue) { },
				_subIndex: -1,
				get subIndex() {  //修复 subIndex 无法及时获取的问题
					//if (this._subIndex === -1) this._subIndex = target.voiceList.map(item => item.name).indexOf(target.voiceName);
					return target.voiceList.indexOf(target.utter.voice);
				},
				set subIndex(newValue) { this._subIndex = newValue; },
				end: function () {
					if (!target.voiceList[this._subIndex]) return;
					target.voiceName = target.voiceList[this._subIndex].name;
				}
			}
		]
	}

	//切换到下一个参数选项
	UtterController.prototype.nextOption = function () {
		this.index++;
		if (this.index >= this.options.length) this.index = 0;
		this.outputInfo();
	};
	//切换到前一个参数选项
	UtterController.prototype.previousOption = function () {
		this.index--;
		if (this.index < 0) this.index = this.options.length - 1;
		this.outputInfo();
	};
	//增大当前选项的值
	UtterController.prototype.valuePlus = function () {
		debugger;
		let key = this.options[this.index].key;
		let maxValue = this.options[this.index].maxValue;
		let step = this.options[this.index].step;
		if (typeof this.target[key] == "object") {
			let op = this.options[this.index];
			let temp = op.subIndex + 1;
			op.subIndex = temp > maxValue ? maxValue : temp;
			op.end();
		} else {
			let temp = (this.target[key] + step).toFixed(2) - 0;
			this.target[key] = temp > maxValue ? maxValue : temp;
		}
		this.outputInfo();
	};
	//减小当前选项的值
	UtterController.prototype.valueMinus = function () {
		let key = this.options[this.index].key;
		let minValue = this.options[this.index].minValue;
		let step = this.options[this.index].step;
		if (typeof this.target[key] == "object") {
			let op = this.options[this.index];
			let temp = op.subIndex - 1;
			op.subIndex = temp < minValue ? minValue : temp;
			op.end();
		} else {
			let temp = (this.target[key] - step).toFixed(2) - 0;
			this.target[key] = temp < minValue ? minValue : temp;
		}
		this.outputInfo();
	};
	//输出更新后的信息
	UtterController.prototype.outputInfo = function () {
		debugger;
		let key = this.options[this.index].key;
		let name = this.options[this.index].name;
		if (typeof this.target[key] == "object") {
			let subIndex = this.options[this.index].subIndex;
			let subKey = this.options[this.index].subKey;
			try {
				this.callback(name + (this.target[key][subIndex] && this.target[key][subIndex][subKey]));
			} catch (err) {
				console.log(err);
			}

		} else {
			this.callback(name + this.target[key]);
		}
	}
	//用于添加热键的回调函数
	UtterController.prototype._hotkeyFunc = function (e) {
		//过滤按键
		if (e.ctrlKey || e.altKey || !e.shiftKey) return;
		//匹配按键
		switch (e.keyCode) {
			case 112:  //F1 
				this.previousOption();
				break;
			case 113:
				debugger;
				this.nextOption();
				break;
			case 114:
				this.valueMinus();
				e.preventDefault();
				break;
			case 115:
				this.valuePlus();
				break;
			default:
				return;
		}
	}


	/**
	 * @description 更新 语音角色
	 * @param {object} tts 
	 * @returns false: 失败 true: 成功
	 */
	function updateVoice(tts) {
		//检查参数，若数据未就绪，则取消执行
		if (!tts.voiceName || tts.voiceList.length == 0) return false;
		//根据语音名称进行查询，若查询失败则取消执行
		let index = tts.voiceList.map(item => item.name).indexOf(tts.voiceName);
		if (index == -1) return false;
		let rate = tts.rate;  //缓存旧的语速值
	tts.utter.voice = tts.voiceList[index];
	tts.rate = rate;  //切换语音后重新赋值，解决本地语音与在线语音切换后语速不同步的问题
		return true;
	}

})();