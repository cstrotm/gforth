\ startup stuff
." load terminal-server" cr stdout flush-file throw
require unix/terminal-server.fs
." load android" cr stdout flush-file throw
require android.fs
." load gl-terminal" cr stdout flush-file throw
require gl-terminal.fs
." done loading" cr stdout flush-file throw
>screen
: t get-connection ;