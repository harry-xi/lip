// Package cmdlip is the entry point of the lip command.
package cmdlip

import (
	"flag"
	"os"
	"path/filepath"

	cmdinstall "github.com/liteldev/lip/cmd/install"
	cmdtooth "github.com/liteldev/lip/cmd/tooth"
	context "github.com/liteldev/lip/context"
	logger "github.com/liteldev/lip/utils/logger"
)

// FlagDict is a dictionary of flags.
type FlagDict struct {
	helpFlag    bool
	versionFlag bool
}

const helpMessage = `
Usage:
  lip [options]
  lip <command> [subcommand options] ...

Commands:
  cache                       Inspect and manage Lip's cache. (TO-DO)
  config                      Manage local and global configuration. (TO-DO)
  install                     Install a tooth.
  list                        List installed teeth. (TO-DO)
  show                        Show information about installed teeth. (TO-DO)
  tooth                       Maintain a tooth.
  uninstall                   Uninstall a tooth. (TO-DO)

Options:
  -h, --help                  Show help.
  -V, --version               Show version and exit.`

const versionMessage = "Lip %s from %s"

// Run is the entry point of the lip command.
func Run() {
	// If there is no argument, print help message and exit.
	if len(os.Args) == 1 {
		logger.Info(helpMessage)
		return
	}

	// If there is a subcommand, run it and exit.
	if len(os.Args) >= 2 {
		switch os.Args[1] {
		case "cache":
			// TO-DO
		case "config":
			// TO-DO
		case "install":
			cmdinstall.Run()
			return
		case "list":
			// TO-DO
		case "show":
			// TO-DO
		case "tooth":
			cmdtooth.Run()
			return
		case "uninstall":
			// TO-DO
		}
	}

	flagSet := flag.NewFlagSet("lip", flag.ExitOnError)

	// Rewrite the default usage message.
	flagSet.Usage = func() {
		logger.Info(helpMessage)
	}

	// Parse flags.

	var flagDict FlagDict

	flagSet.BoolVar(&flagDict.helpFlag, "help", false, "")
	flagSet.BoolVar(&flagDict.helpFlag, "h", false, "")

	flagSet.BoolVar(&flagDict.versionFlag, "version", false, "")
	flagSet.BoolVar(&flagDict.versionFlag, "V", false, "")

	flagSet.Parse(os.Args[1:])

	// Help flag has the highest priority.
	if flagDict.helpFlag {
		logger.Info(helpMessage)
		return
	}

	if flagDict.versionFlag {
		exPath, _ := filepath.Abs(os.Args[0])
		logger.Info(versionMessage, context.Version.String(), exPath)
		return
	}
}
