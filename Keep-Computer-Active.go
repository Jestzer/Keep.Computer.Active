package main

import (
	"fmt"
	"os"
	"runtime"
	"time"

	"github.com/go-vgo/robotgo"
)

func main() {

	// Print version number, if requested.
	args := os.Args[1:]
	for _, arg := range args {
		if arg == "-version" {
			fmt.Println("Version number: 1.2")
			os.Exit(0)
		}
	}

	for {
		// Linux gets angerryyyy when you try to use F13.
		if runtime.GOOS == "linux" {
			robotgo.KeyTap("scrolllock")
			robotgo.KeyTap("scrolllock")
		} else {
			robotgo.KeyTap("f13")
		}

		// I'm sorry, what kind of non-sensical bullshit is way of outputting time? This is absolutely insane.
		// Why would putting in a random time make more sense than writing HH:MM:SS AM/PM??
		fmt.Println("Continuing to keep computer active. See you in 3 minutes! Current time is", time.Now().Local().Format("03:04:05 PM."))

		// Wait for 3 minutes.
		time.Sleep(3 * time.Minute)
	}
}
