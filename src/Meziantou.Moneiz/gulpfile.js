/// <binding ProjectOpened='watch' />
"use strict";

var gulp = require("gulp");
var rename = require("gulp-rename");

function css() {
    var postcss = require("gulp-postcss");
    var postcssimport = require("postcss-import");
    var autoprefixer = require("autoprefixer");
    var normalize = require("postcss-normalize");
    var customSelectors = require("postcss-custom-selectors");
    var cssnano = require("cssnano");
    var sourcemaps = require('gulp-sourcemaps');

    return gulp.src("./wwwroot/css/site.css")
        .pipe(sourcemaps.init())
        .pipe(postcss([
            postcssimport(),
            customSelectors(),
            normalize(),
            autoprefixer({
                add: true,
                remove: true
            }),
            cssnano({
                autoprefixer: false // already done
            })
        ]))
        .pipe(rename({ suffix: '.min' }))
        .pipe(sourcemaps.write("."))
        .pipe(gulp.dest("./wwwroot/css/"));
}

function watchCss() {
    gulp.watch(["./wwwroot/css/*.css", "!./wwwroot/css/*.min.css", "!./wwwroot/css/*.min.css.map"], css);
}

exports.watch = gulp.parallel(css, watchCss);
exports.default = gulp.parallel(css);
