/// <binding ProjectOpened='watch' />
"use strict";

var gulp = require("gulp");

function css() {
    var rename = require("gulp-rename");
    var postcss = require("gulp-postcss");
    var postcssimport = require("postcss-import");
    var cssnano = require("cssnano");

    return gulp.src("./wwwroot/css/site.css")
        .pipe(postcss([
            postcssimport(),
            cssnano({
                autoprefixer: false
            })
        ]))
        .pipe(rename({ suffix: '.min' }))
        .pipe(gulp.dest("./wwwroot/css/"));
}

function watchCss() {
    gulp.watch(["./wwwroot/css/*.css", "!./wwwroot/css/*.min.css", "!./wwwroot/css/*.min.css.map"], css);
}

exports.watch = gulp.parallel(css, watchCss);
exports.default = gulp.parallel(css);
