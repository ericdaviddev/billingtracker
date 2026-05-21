--
-- PostgreSQL database dump
--

\restrict Dq4MW9BV1LUMkc4d13CgF6Vdt74DqSwrXI49pI02NhJiW06iSYrvof860qe7ZUy

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
-- Name: locations; Type: TABLE; Schema: billing; Owner: postgres
--

CREATE TABLE billing.locations (
    location_id uuid DEFAULT gen_random_uuid() NOT NULL,
    location_name text NOT NULL,
    client_id uuid NOT NULL,
    managed_by_platform boolean DEFAULT true NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


ALTER TABLE billing.locations OWNER TO postgres;

--
-- Name: locations locations_pkey; Type: CONSTRAINT; Schema: billing; Owner: postgres
--

ALTER TABLE ONLY billing.locations
    ADD CONSTRAINT locations_pkey PRIMARY KEY (location_id);


--
-- Name: locations locations_clients_client_id_fk; Type: FK CONSTRAINT; Schema: billing; Owner: postgres
--

ALTER TABLE ONLY billing.locations
    ADD CONSTRAINT locations_clients_client_id_fk FOREIGN KEY (client_id) REFERENCES billing.clients(client_id);


--
-- PostgreSQL database dump complete
--

\unrestrict Dq4MW9BV1LUMkc4d13CgF6Vdt74DqSwrXI49pI02NhJiW06iSYrvof860qe7ZUy

