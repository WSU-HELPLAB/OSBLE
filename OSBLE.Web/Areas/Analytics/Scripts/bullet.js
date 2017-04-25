(function () {

    // Chart design based on the recommendations of Stephen Few. Implementation
    // based on the work of Clint Ivy, Jamie Love, and Jason Davies.
    // http://projects.instantcognition.com/protovis/bulletchart/
    // yw. This chart has become a stacked chart from the original bullet chart
    d3.bullet = function () {
        var orient = "left",
            markers = bulletMarkers,
            measures = bulletMeasures,
            width = 380,
            height = 50,
            tickFormat = d3.time.format("%m/%d/%y, %H:%M"),
            timeScale = 20,
            numTicks = 10,
            tickScale = 1;

        // For each small multiple…
        function bullet(g) {
            g.each(function (d, i) {

                if (d.showTicks) this.parentNode.height.baseVal.value += 50;

                var markerz = markers.call(this, d, i).slice(),
                    measurez = measures.call(this, d, i).slice(),
                    measurezR = measures.call(this, d, i).slice().reverse(), // using the last segment's endpoint position to calculate the chart scales
                    g = d3.select(this);

                // Compute the new x-scale.

                
                var x1 = d3.scale.linear()
                                 .domain([0, measurezR[0].endPoint])
                                 .range([0, measurezR[0].endPoint * timeScale]);

                var tickscale = d3.time.scale()
                                 .domain([measurez[0].startTicks, measurezR[0].endTicks])
                                 .range([0, measurezR[0].endPoint * timeScale]);



                // Retrieve the old x-scale, if this is an update.
                var x0 = this.__chart__ || d3.scale.linear()
                                                   .domain([0, Infinity])
                                                   .range(x1.range());

                // Stash the new scale
                this.__chart__ = x1;

                // Derive starting points and width-scales from the x-scales for segments.
                var px0 = wbulletStart(x0),
                    px1 = wbulletStart(x1),
                    ww0 = wbulletWidth(x0),
                    ww1 = wbulletWidth(x1),
                    tx0 = bulletText(x0),
                    tx1 = bulletText(x1);

                // Derive width-scales from the x-scales for markers.
                var w0 = bulletWidth(x0),
                    w1 = bulletWidth(x1);

                // Update the measure rects.
                var measure = g.selectAll("rect.measure")
                    .data(measurez);
                var measureEnter = measure.enter().append("g");
                measureEnter.append("rect")
                    .attr("class", function (d, i) { return "measure " + measurez[i].cssClass; })
                    .attr("width", ww0)
                    .attr("height", height / 3)
                    .attr("x", px0)
                    .attr("y", height / 3)
                    .attr("onmousemove", "Chart.showTooltip(evt)")
                    .attr("onmouseout", "Chart.hideTooltip(evt)")
                  .transition()
                    .attr("width", ww1)
                    .attr("x", px1);

                // rect labels
                if (tickScale <= 1) {
                    measureEnter.append("text")
                        .attr("class", "rect-label")
                        .attr("x", tx0)
                        .attr("y", height * 7 / 12)
                        .text(function (d) {
                            return d.name;
                        })
                      .transition()
                        .attr("width", ww1)
                        .attr("x", tx1);
                }

                measureEnter.append("text")
                 .attr("class", "tooltipT")
                 .attr("visibility", "hidden")
                 .text(function (d, i) { return measurez[i].timeRangeDisplayText; });

                // Update the marker lines.
                var marker = g.selectAll("line.marker")
                    .data(markerz);

                marker.enter().append("line")
                    .attr("class", "marker")
                    .attr("data-label", function (d, i) { return markerz[i].name; })
                    .attr("x1", w0)
                    .attr("x2", w0)
                    .attr("y1", height / 6)
                    .attr("y2", height * 5 / 6)

                marker.transition()
                    .attr("x1", w1)
                    .attr("x2", w1)
                    .attr("y1", height / 6)
                    .attr("y2", height * 5 / 6);

                if (d.showTicks) {
                    // Compute the tick format.
                    var format = tickFormat || tickscale.tickFormat(8);
                    // Update the tick groups.
                    var tick = g.selectAll("g.tick")
                        .data(tickscale.ticks(numTicks), function (d) {
                            return this.textContent || format(d);
                        });

                    // Initialize the ticks with the old scale, x0.
                    var tickEnter = tick.enter().append("g")
                        .attr("class", "tick")
                        .attr("transform", bulletTranslate(x0))
                        .style("opacity", 1e-6);

                    tickEnter.append("line")
                        .attr("y1", height)
                        .attr("y2", height * 7 / 6);

                    if (tickScale > 60) format = d3.time.format("%m/%d/%y");

                    tickEnter.append("text")
                        .attr("text-anchor", "middle")
                        .attr("dy", "1em")
                        .attr("y", height * 7 / 6)
                        .text(format);
                        

                    if (tickScale <= 1) {
                        tickEnter.select("text")
                        .attr("visibility", "hidden")
                        .attr("onmouseout", "Chart.hideTooltipTK(evt)");

                        tickEnter.append("text")
                            .attr("text-anchor", "middle")
                            .attr("dy", "1em")
                            .attr("y", height * 7 / 6)
                            .text(d3.time.format("%H:%M"))
                            .attr("onmousemove", "Chart.showTooltipTK(evt)");
                    }


                    // Transition the entering ticks to the new scale, tickscale.
                    tickEnter.transition()
                        .attr("transform", bulletTranslate(tickscale))
                        .style("opacity", 1);

                    // Transition the updating ticks to the new scale, tickscale.
                    var tickUpdate = tick.transition()
                        .attr("transform", bulletTranslate(tickscale))
                        .style("opacity", 1);

                    tickUpdate.select("line")
                        .attr("y1", height)
                        .attr("y2", height * 7 / 6);

                    tickUpdate.select("text")
                        .attr("y", height * 7 / 6);

                    // Transition the exiting ticks to the new scale, tickscale.
                    tick.exit().transition()
                        .attr("transform", bulletTranslate(tickscale))
                        .style("opacity", 1e-6)
                        .remove();
                }
            });
            d3.timer.flush();
        }

        // left, right, top, bottom
        bullet.orient = function (x) {
            if (!arguments.length) return orient;
            orient = x;
            reverse = orient == "right" || orient == "bottom";
            return bullet;
        };

        // markers (previous, goal)
        bullet.markers = function (x) {
            if (!arguments.length) return markers;
            markers = x;
            return bullet;
        };

        // measures (actual, forecast)
        bullet.measures = function (x) {
            if (!arguments.length) return measures;
            measures = x;
            return bullet;
        };

        bullet.width = function (x) {
            if (!arguments.length) return width;
            width = x;
            return bullet;
        };

        bullet.height = function (x) {
            if (!arguments.length) return height;
            height = x;
            return bullet;
        };

        bullet.timeScale = function (x) {
            if (!arguments.length) return timeScale;
            timeScale = x;
            return bullet;
        };

        bullet.numTicks = function (x) {
            if (!arguments.length) return numTicks;
            numTicks = x;
            return bullet;
        };

        bullet.tickScale = function (x) {
            if (!arguments.length) return tickScale;
            tickScale = x;
            return bullet;
        };

        return bullet;
    };

    function bulletMarkers(d) {
        return d.markers;
    }

    function bulletMeasures(d) {
        return d.measures;
    }

    function bulletTranslate(x) {
        return function (d) {
            return "translate(" + x(d) + ",0)";
        };
    }

    function wbulletStart(x) {
        return function (d) {
            return Math.abs(x(d.startPoint) - x(0));
        };
    }

    function wbulletWidth(x) {
        return function (d) {
            return Math.abs(x(d.endPoint) - x(d.startPoint) - x(0));
        };
    }

    function bulletWidth(x) {
        var x0 = x(0);
        return function (d) {
            return Math.abs(x(d.position) - x0);
        };
    }

    function bulletText(x) {
        return function (d) {
              return Math.abs(x(d.startPoint) - x(0) + 6);
        };
    }

})();