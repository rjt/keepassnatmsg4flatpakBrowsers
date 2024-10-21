using System;
using System.Collections.Generic;
using System.IO;

namespace KeePassNatMsg.NativeMessaging
{
    public abstract class PosixHost : NativeMessagingHost
    {
        private const string PosixScript = """
            #!/bin/bash
            THISSCRIPT="${0}";
            BASHSOURCE="${BASH_SOURCE[0]}";
            BASHSOURCE="${BASHSOURCE%/*}";  #path of this source file
            #LOGPATH="dirname ${THISSCRIPT}"; #dirname is a standalone binary, not a bash built-in, so is not available inside the flatpak.
            LOGPATH="${THISSCRIPT%/*}";  #dirname equivalent ... Do everything within bash.
            LOGFILE="${THISSCRIPT##*/}.log";  #basename equivalent
            LOG="${LOGPATH}/${LOGFILE}";
            #LOG=/dev/null
            OLD_PWD="${PWD}";
            DT=$(date +%Y%m%d-%H%M%S);

            exec 3>> "${LOG}";
            MONOVERBOSE=' --verbose '; # NOW BREAKS LAUNCH
            MONOVERBOSE='';
            MONO_LOG_LEVEL='debug'; #ENVIRONMENT VARIABLE SET BEFORE CALLING MONO BINARY.
            #MONO_LOG_LEVEL='';
            export MONO_LOG_LEVEL
            export MONODEBUG
            export MONO_IOMAP=all
            #--optimize=OPT precomp is precompile
            # ` mono --list-opt ` #lists optimizations to pass to --optimize
            MONOOPTIMIZE=' --optimize=precomp ';
            MONOOPTIMIZE='';
            #KPDEBUG=' --debug ';
            KPDEBUG='';

            echo "                                   " >&3;  #DONT ECHO BLANKS AS IT MAY INTERFERE WITH NATIVE_MESSAGING_HOSTS "type": "stdio" !!!!
            echo "              DT: ${DT}            " >&3;
            echo "         OLD_PWD: ${OLD_PWD}       " >&3;
            echo "             PWD: ${PWD}           " >&3;
            echo "       THISCRIPT: ${THISSCRIPT}    " >&3;
            echo "         LOGPATH: ${LOGPATH}       " >&3;
            echo "             LOG: ${LOG}           " >&3;

            #Neither dirname nor readlink commands are available inside flatpak container, so use flatpak_command_exists_posix() below. 
            #KPNatMsgDir=$(dirname $(readlink -f ~/.keepassnatmsg/run-proxy.sh) ) # ${HOME} is undefined inside flatpak, so ~ might fail in some environments.
            #KPNatMsgDir=$(dirname "$(readlink -f "${THISSCRIPT}")" )   # ToDo: stress test the double quotes here.  Should they be escaped first.  
            KPNatMsgDir="${BASHSOURCE}"
            echo "     KPNatMsgDir: ${KPNatMsgDir} " >&3;
            cd "${KPNatMsgDir}" || return >&3;

            echo "         OLD_PWD: ${OLD_PWD}       " >&3;
            echo "             PWD: ${PWD}           " >&3;
            echo "     MONOVERBOSE: ${MONOVERBOSE}   " >&3;
            echo "  MONO_LOG_LEVEL: ${MONO_LOG_LEVEL}" >&3;
            echo "    MONOOPTIMIZE: ${MONOOPTIMIZE}  " >&3;
            echo "      MONO_IOMAP: ${MONO_IOMAP}    " >&3;
            echo "         KPDEBUG: ${KPDEBUG}       " >&3;

            flatpak_command_exists_posix() {
                if command -v flatpak >/dev/null; then
                    echo 'flatpak exists, so we are running directly on host, not inside flatpak container.' >&3;
                    FLATPAK_PREFIX='';
                elif command -v flatpak-spawn >/dev/null; then
                    echo '"flatpak-spawn" exists, so we are inside a flatpak container.' >&3;
                    echo '   --host \
                                Run the command unsandboxed on the host. This requires access to the\
                                org.freedesktop.Flatpak D-Bus interface.'  >&3;

                    FLATPAK_PREFIX='flatpak-spawn --host '; #NOTE, PURPOSELY ADDING SPACEBAR.
                else 
                    echo Flatpak not installed here.  >&3;
                    FLATPAK_PREFIX='';
                fi;
            }
            flatpak_command_exists_posix;
            echo "FLATPAK_PREFIX : \"${FLATPAK_PREFIX}\"   flatpak_command_exists_posix() " >&3;
            #flatpak-spawn --host mono keepassnatmsg-proxy.exe

            mono_command_exists_posix() {
                if command -v mono >/dev/null; then
                    echo 'mono exists, so we are probably running directly on host, not inside mono container.' >&3;
                    MONO_PREFIX='';
                elif command -v mono-spawn >/dev/null; then
                    echo '"mono-spawn" exists, so we are inside a mono container.' >&3;
                    echo '   --host \
                                Run the command unsandboxed on the host. This requires access to the\
                                org.freedesktop.mono D-Bus interface.'  >&3;

                    MONO_PREFIX='mono-spawn --host '; #NOTE, PURPOSELY ADDING SPACEBAR.
                else 
                    echo mono not installed here.  >&3;
                    MONO_PREFIX='';
                fi;
            }
            #mono_command_exists_posix;
            echo "MONO_PREFIX : \"${MONO_PREFIX}\"   mono_command_exists_posix() " >&3;

                        
            #set -x;
            #cd KPNatMsgDir
            #mono keepassnatmsg-proxy.exe
            #popd   
            echo "${MONOVERBOSE} ${OPTIMIZE} ${EXEFQFN} ${KPDEBUG} ${KDBXFQFN}" >&3;
            echo 'MONO_LOG_LEVEL="debug" MONO_LOG_MASK="dll" mono glue.exe' >&3; 
            echo 'MONO_LOG_LEVEL="${MONOVERBOSE}" MONO_LOG_MASK="dll" "${KPNatMsgDir}"/keepassnatmsg-proxy.exe' >&3;
            echo "MONO_LOG_LEVEL=\"${MONO_LOG_LEVEL}\" MONO_LOG_MASK=\"dll\" \"${KPNatMsgDir}\"/keepassnatmsg-proxy.exe" >&3;  

            if [[ -f mono && -x mono ]]; then 
            #if [[ -x readlink && -x $(readlink -f $(which mono)) ]]; then
                echo "FOUND mono binary :)" >&3; 
                ${FLATPAK_PREFIX} mono "${MONOVERBOSE}" "${KPNatMsgDir}"/keepassnatmsg-proxy.exe
            else 
                echo "ToDo: mono binary NOT FOUND!  Which is expected when in flatpak container! " >&3; 
                echo "ToDo: Install mono under home directory or at least someplace else AND " >&3; 
                echo "ToDo: pass ENV PATH TO MONO ON flatpak spawn call " >&3; 
                set -x >&3; #MUST TURN OFF IN PRODUCTION TO NOT POLLUTE STDIO COMMUNICATIONS.
                #    mono {0}
                ${FLATPAK_PREFIX} mono ${MONOVERBOSE} "${KPNatMsgDir}"/keepassnatmsg-proxy.exe   #DO NOT DOUBLE QUOTE ${MONOVERBOSE} AS IT BREAKS LAUNCH!!!
                #echo "Exiting 6 ! " >&3; 
                #exit 6;
                set +x >&3;
            fi;

            """;
        private const string PosixProxyPath = ".keepassnatmsg";

        private string _home = Environment.GetEnvironmentVariable("HOME");

        public override string ProxyPath
        {
            get
            {
                return Path.Combine(_home, PosixProxyPath);
            }
        }

        protected abstract string[] BrowserPaths { get; }

        public override void Install(Browsers browsers)
        {
            InstallPosix(browsers);
        }

        public override Dictionary<Browsers, BrowserStatus> GetBrowserStatuses()
        {
            var statuses = new Dictionary<Browsers, BrowserStatus>();
            var i = 0;

            foreach (Browsers b in Enum.GetValues(typeof(Browsers)))
            {
                if (b != Browsers.None)
                {
                    var status = BrowserStatus.NotInstalled;
                    var jsonFile = Path.Combine(_home, BrowserPaths[i], string.Format("{0}.json", GetExtKey(b)));
                    var jsonDir = Path.GetDirectoryName(jsonFile);
                    var jsonDirInfo = new DirectoryInfo(jsonDir);
                    var jsonParent = jsonDirInfo.Parent.FullName;

                    if (Directory.Exists(jsonParent))
                    {
                        status = BrowserStatus.Detected;
                    }

                    if (File.Exists(jsonFile))
                    {
                        status = BrowserStatus.Installed;
                    }
                    statuses.Add(b, status);
                }
                i++;
            }

            return statuses;
        }

        protected void InstallPosix(Browsers browsers)
        {
            if (!Directory.Exists(ProxyPath))
            {
                Directory.CreateDirectory(ProxyPath);
            }
            var monoScript = Path.Combine(ProxyPath, "run-proxy.sh");
            File.WriteAllText(monoScript, string.Format(PosixScript, ProxyExecutable), _utf8);

            Mono.Unix.Native.Stat st;
            Mono.Unix.Native.Syscall.stat(monoScript, out st);
            if (!st.st_mode.HasFlag(Mono.Unix.Native.FilePermissions.S_IXUSR))
            {
                Mono.Unix.Native.Syscall.chmod(monoScript, Mono.Unix.Native.FilePermissions.S_IXUSR | st.st_mode);
            }

            var i = 0;

            foreach (Browsers b in Enum.GetValues(typeof(Browsers)))
            {
                if (b != Browsers.None && browsers.HasFlag(b))
                {
                    var jsonFile = Path.Combine(_home, BrowserPaths[i], string.Format("{0}.json", GetExtKey(b)));
                    var jsonDir = Path.GetDirectoryName(jsonFile);

                    var jsonDirInfo = new DirectoryInfo(jsonDir);
                    var jsonParent = jsonDirInfo.Parent.FullName;

                    if (Directory.Exists(jsonParent))
                    {
                        if (!Directory.Exists(jsonDir))
                        {
                            Directory.CreateDirectory(jsonDir);
                        }
                        File.WriteAllText(jsonFile, string.Format(GetJsonData(b), monoScript), _utf8);
                    }
                }
                i++;
            }
        }
    }
}
