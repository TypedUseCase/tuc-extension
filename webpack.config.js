var path = require("path");
var webpack = require("webpack");
var fableUtils = require("fable-utils");
var nodeExternals = require('webpack-node-externals');

function resolve(filePath) {
  return path.join(__dirname, filePath)
}


var babelOptions = fableUtils.resolveBabelOptions({
  presets: [["es2015", { "modules": false }]],
  plugins: ["transform-runtime"]
});

var isProduction = process.argv.indexOf("-p") >= 0;
console.log("Bundling for " + (isProduction ? "production" : "development") + "...");

module.exports = function (env) {

  var outputPath = "release";
  console.log("Output path: " + outputPath);

  var compilerDefines = isProduction ? [] : ["DEBUG"];

  return {
    target: 'node',
    devtool: "source-map",
    entry: resolve('./src/Extension.fsproj'),
    output: {
      filename: 'extension.js',
      path: resolve('./' + outputPath),
      //library: 'IONIDEFSHARP',
      libraryTarget: 'commonjs'
    },
    resolve: {
      modules: [resolve("./node_modules/")]
    },
    //externals: [nodeExternals()],
    externals: {
      // Who came first the host or the plugin ?
      "vscode": "commonjs vscode",

      // Optional dependencies of ws
      "utf-8-validate": "commonjs utf-8-validate",
      "bufferutil": "commonjs bufferutil"
    },
    module: {
      rules: [
        {
          test: /\.fs(x|proj)?$/,
          use: {
            loader: "fable-loader",
            options: {
              babel: babelOptions,
              define: compilerDefines
            }
          }
        },
        {
          test: /\.js$/,
          exclude: /node_modules/,
          use: {
            loader: 'babel-loader',
            options: babelOptions
          },
        }
      ]
    }
  };
}
