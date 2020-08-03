#!/usr/bin/gnuplot
set term png size 1920,1080
set title "Trump tweet deltas\nAs a percentage of total posts"
set auto x
set yrange [.2:1.2]
set style data histogram
set style fill solid border -1
#set bmargin 10 
plot 'plot.dat' using 2:xtic(1) ti col, '' u 3 ti col, '' u 4 ti col