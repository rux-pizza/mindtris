SHELL = /bin/sh
SYSTEM = $(shell uname)
C++ = g++
CC = gcc
DFLAGS =
OFLAGS = -O3
LFLAGS = -L. -L./Crypto++/ -lcryptopp -L./mindtriscore/ -lmindtriscore


CLIENTDIR = MindTris++
SERVERDIR = mt_server

SUBDIRS = $(CLIENTDIR) $(SERVERDIR)

ifeq ($(SYSTEM),Darwin)
DFLAGS += -D__APPLE__
OFLAGS += -flat_namespace
else
LFLAGS += -lrt
endif

ifeq ($(SYSTEM),FreeBSD)
DFLAGS += -D__FREEBSD__
endif

ifeq ($(SYSTEM),SunOS)
DFLAGS += -D__SOLARIS__
LFLAGS += -lresolv -lsocket -lnsl
endif

CLIENTEXEC = mindtris++
SERVEREXEC = mindtris_server

CLIENTOBJS = $(CLIENTDIR)/peer.o $(CLIENTDIR)/mindtris.o
CLIENTLFLAGS = $(LFLAGS) -lcurses

SERVEROBJS = $(SERVERDIR)/database.o $(SERVERDIR)/lobby.o $(SERVERDIR)/$(SERVERDIR).o $(SERVERDIR)/user.o
SERVERLFLAGS = $(LFLAGS)

PROGS = $(CLIENTEXEC) $(SERVEREXEC)

all: $(PROGS)
	@echo
	@echo "#######################################"
	@echo "### BUILDING ALL TARGETS ###"
	@echo "#######################################"
	@echo 
	for i in $(SUBDIRS) ; do \
	( cd $$i ; make ) ; \
	done

$(CLIENTEXEC): $(CLIENTOBJS) $(COBJS)
	$(C++) -o $(CLIENTEXEC) $(CLIENTOBJS) $(CLIENTLFLAGS)

$(SERVEREXEC): $(SERVEROBJS) $(COBJS)
	$(C++) -o $(SERVEREXEC) $(SERVEROBJS) $(SERVERLFLAGS) 

clean:
	rm -f $(OBJS) $(COBJS) $(PROGS)

$(OBJS): %.o: %.cpp
	$(C++) -o $@ $(CFLAGS) -c $<

$(COBJS): %.o: %.c
	$(CC) -o $@ $(CFLAGS) -c $<



all: $(PROGS)

