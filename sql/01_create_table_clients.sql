--
-- PostgreSQL database dump
--

\restrict sjDJ8odqNc89E2BJHv0XVLeLWKFm2RM6y6cCpUVsZxAhkvC1DpjPynKcpTghBCk

-- Dumped from database version 18.3
-- Dumped by pg_dump version 18.3

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET transaction_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: clients; Type: TABLE; Schema: billing; Owner: postgres
--

CREATE TABLE billing.clients (
    client_id uuid DEFAULT gen_random_uuid() NOT NULL,
    client_name text NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


ALTER TABLE billing.clients OWNER TO postgres;

--
-- Name: clients clients_pkey; Type: CONSTRAINT; Schema: billing; Owner: postgres
--

ALTER TABLE ONLY billing.clients
    ADD CONSTRAINT clients_pkey PRIMARY KEY (client_id);


--
-- PostgreSQL database dump complete
--

\unrestrict sjDJ8odqNc89E2BJHv0XVLeLWKFm2RM6y6cCpUVsZxAhkvC1DpjPynKcpTghBCk

