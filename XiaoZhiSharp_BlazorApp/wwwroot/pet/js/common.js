/*
 * @Author: liyanan liyn@mail.open.com.cn
 * @Date: 2024-08-16 15:45:30
 * @LastEditors: liyanan liyn@mail.open.com.cn
 * @LastEditTime: 2024-08-19 16:28:32
 * @FilePath: /frontend-pet/js/common.js
 * @Description: 这是默认设置,请设置`customMade`, 打开koroFileHeader查看配置 进行设置: https://github.com/OBKoro1/koro1FileHeader/wiki/%E9%85%8D%E7%BD%AE
 */
function getUrlParams(name) {
    // 获取当前URL中的查询字符串
    const queryString = window.location.search.substring(1);
    const params = {};

    if (name) {
        // 如果提供了参数名，使用正则表达式获取该参数的值
        const regex = new RegExp(name + '=([^&#]*)');
        const results = regex.exec(queryString);
        return results ? decodeURIComponent(results[1]) : null;
    } else {
        // 如果没有提供参数名，解析所有参数
        const paramsArray = queryString.split('&');
        paramsArray.forEach((param) => {
            const [key, value] = param.split('=');
            params[key] = decodeURIComponent(value);
        });
        return params;
    }
}

/**
 * 格式化时间戳为易读的格式
 * @param {number} timestamp - 时间戳（单位：毫秒）
 * @return {string} - 格式化后的时间字符串
 */
function formatTimestamp(timestamp) {
    const date = new Date(timestamp);
    const hours = padZero(date.getHours());
    const minutes = padZero(date.getMinutes());
    // 可以根据需要添加更多的时间单位，如日期、秒等
    return `${hours}:${minutes}`;
}

/**
 * 确保数字有两位，如果不足两位则在前面补零
 * @param {number} num - 数字
 * @return {string} - 补零后的字符串
 */
function padZero(num) {
    return num.toString().padStart(2, '0');
}